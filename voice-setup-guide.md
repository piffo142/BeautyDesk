# Voice Booking Channel — Setup Guide
**Companion to:** prd.md (SIG BeautyDesk)
**Covers:** PRD Section 6.4 / FR10–FR12
**Stack:** Twilio + Vapi + n8n + SIG.BeautyDesk.Api

Assumption made: **Vapi** chosen over Retell for this walkthrough — it has the more mature n8n function-calling template set as of writing. Swap-in for Retell follows the same shape; only the agent-config screens differ. Revisit once the PRD's open question on pricing/latency is settled.

---

## 0. Prerequisites

- `SIG.BeautyDesk.Api` deployed somewhere reachable over HTTPS (even a dev tunnel like `ngrok`/Cloudflare Tunnel for initial testing — Twilio and Vapi both need public webhook URLs).
- `GetAvailability`, `IdentifyOrCreateCustomer`, `CreateBooking`, `EscalateToHuman` endpoints built per FR10, each accepting/returning JSON, secured per NFR2 (API key + HMAC).
- n8n instance running (cloud or self-hosted) reachable over HTTPS.
- Twilio account, Vapi account.

---

## 1. Twilio number

1. Twilio Console → **Phone Numbers → Buy a Number** → pick a UK local or 0800 number.
2. Under the number's **Voice Configuration**, set "A call comes in" → **Webhook**, leave this blank for now — you'll point it at Vapi in Step 2, not directly at n8n. Vapi sits between Twilio and n8n.
3. Note the number's SID and the number itself — both go into `CallLog.FromNumber` matching later.
4. Enable call recording at the trunk level if you want full audio retained (Console → Voice → Recording) — required for FR11's audit trail. Set retention/storage per NFR4 before going live, not after.

---

## 2. Vapi agent

1. Vapi dashboard → **Create Assistant**.
2. **Model**: pick your LLM (GPT-4o-class or equivalent — function-calling reliability matters more here than raw quality).
3. **Transcriber**: default Deepgram is fine for UK English; test with regional accents before committing.
4. **System prompt** — keep it task-scoped, not a generic chatbot persona. Example skeleton:

   ```
   You are the booking assistant for [Salon Name]. You can:
   - Check appointment availability for a requested service and date/time.
   - Book an appointment once the customer confirms a specific slot.
   - Look up or create a customer record by phone number.
   - Escalate to a human if the request is unclear, a complaint, or you fail twice.

   Rules:
   - Never confirm a booking without explicit verbal confirmation of date, time, and service.
   - If the service requires a patch test and the customer has none on file, tell them the
     booking will be provisional pending an in-salon patch test, and still create it as tentative.
   - Always read back the confirmed slot before ending the call.
   - If you cannot resolve the request within two attempts, call escalate_to_human.
   ```

5. **Phone number**: link the Twilio number from Step 1 directly in Vapi's phone number settings (Vapi manages the Twilio webhook wiring for you here — Console → Phone Numbers → Import from Twilio, using your Twilio Account SID + Auth Token).

---

## 3. Define Vapi function-calling tools

In the Assistant's **Functions/Tools** section, define four tools mapping to the API endpoints from FR10. Each tool's "server URL" points at an **n8n webhook**, not directly at the API — n8n is the intermediary so you can add logging, retries, and the HMAC signing step without changing Vapi config every time.

| Tool name | Purpose | Params sent by agent |
|---|---|---|
| `get_availability` | FR10 — query open slots | `serviceName`, `preferredDateRange` |
| `identify_or_create_customer` | FR10 — phone lookup/create | `phoneNumber`, `name` (if new) |
| `create_booking` | FR10 — confirm booking | `customerId`, `serviceId`, `slotStartUtc` |
| `escalate_to_human` | FR6/FR10 — handover | `reason`, `transcriptSoFar` |

Vapi auto-populates the caller's number into the call context — pass that through as `phoneNumber` rather than asking the customer to repeat it.

---

## 4. n8n workflows

Create one workflow per tool (or one workflow with a switch node keyed on tool name — simpler to maintain at this scale is one workflow, branched).

**4.1 Webhook trigger**
- Node: **Webhook**, method POST, one path per tool (e.g. `/webhook/get-availability`).
- Set Vapi's tool "server URL" to `https://<your-n8n-host>/webhook/get-availability`.

**4.2 HMAC signing before calling the API** (NFR2)
- Node: **Code** (or Crypto node) — compute `HMAC-SHA256(body, sharedSecret)`, attach as a request header (`X-Signature`).
- Store the shared secret in n8n credentials, not hardcoded in the node.

**4.3 Call the API**
- Node: **HTTP Request** → `POST https://<your-api-host>/api/availability` (or the relevant endpoint), headers: `X-Api-Key`, `X-Signature`, body mapped from the webhook payload.

**4.4 Map response back to Vapi's expected tool-result shape**
- Vapi expects a JSON response the LLM can read back conversationally — keep it terse and structured, e.g.:
  ```json
  { "available": true, "slots": ["2026-07-02T10:00:00Z", "2026-07-02T11:30:00Z"] }
  ```
- Node: **Respond to Webhook**, body = the mapped result.

**4.5 Escalation workflow specifically**
- On `escalate_to_human`: call the API's `EscalateToHuman` endpoint (flags the `Enquiry`, per FR6), then trigger a **SignalR** push to reception (this likely needs a small dedicated endpoint on your API that n8n calls, which internally broadcasts via SignalR — n8n itself doesn't speak SignalR natively).
- Optionally, branch: have n8n tell Vapi to perform a warm transfer (`transferCall` action in Vapi, target = your salon's live line) rather than just ending the call.

**4.6 Post-booking SMS confirmation** (FR12)
- This can live in the `create_booking` n8n workflow (call Twilio's SMS API after a successful booking) **or**, cleaner, inside the API itself, triggered server-side on booking creation regardless of channel — preferred, since it then also covers MAUI-originated bookings without duplicating the SMS logic in n8n. Recommend doing it API-side; mention here only because the wiring needs to exist somewhere.

---

## 5. Patch-test gating in practice (FR3, FR10)

The `create_booking` n8n workflow should not need any patch-test logic itself — that check belongs in the API per FR3/NFR1. n8n just passes through the API's response, which will indicate `status: "Tentative"` with a reason if a patch test is missing. Make sure the n8n response mapping (Step 4.4) surfaces that status text so the voice agent can read it back to the customer ("I've pencilled that in, but you'll need a quick patch test before the appointment").

---

## 6. Call logging / audit trail (FR11)

- Vapi sends an **end-of-call webhook** (configure in Assistant settings → Server URL for call events) containing the call summary, transcript, and recording URL.
- Build one more n8n workflow on this webhook: write to `CallLog` via a dedicated API endpoint (`POST /api/call-logs`), capturing `CallSid`, `FromNumber`, `RecordingUrl`, `DurationSec`, `N8nWorkflowExecutionId` (use n8n's built-in `$execution.id`), and `RawTranscriptJson`.
- Link this back to the `Enquiry`/`Booking` created earlier in the same call — easiest if you pass the `CallSid` through as a correlation ID on every tool call in Step 3, so the API can stitch the records together without relying on timing.

---

## 7. Testing checklist before going live

- [ ] Call the Twilio number from a mobile and confirm Vapi answers with the configured greeting.
- [ ] Walk through a full booking for a non-patch-test service end to end; confirm the `Booking` row appears with correct UTC times and the SMS confirmation arrives.
- [ ] Walk through a patch-test-required service; confirm it lands as `Tentative` and the agent communicates that correctly.
- [ ] Force an unclear/garbled request and confirm `escalate_to_human` fires and reception gets a live alert.
- [ ] Attempt to book the same slot simultaneously from the MAUI desktop client and the voice line (two devices/calls) — confirm the race-safety logic from FR2 rejects one of them cleanly rather than double-booking.
- [ ] Confirm `CallLog` entries are appearing with working recording links and correct retention tagging (NFR4).
- [ ] Test with at least two different regional UK accents through the transcriber before trusting it on live customers.

---

## 8. Open items carried over from the PRD

- Confirm Vapi vs Retell choice for real (Section 10 of prd.md) — this guide assumes Vapi; re-derive Steps 2–3 against Retell's tool/function model if you switch.
- Deposit collection isn't covered here — out of scope per PRD Section 3, phase 2.
