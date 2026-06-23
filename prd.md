# PRD — Beauty Salon Helpdesk & Booking Platform
**Working name:** SIG BeautyDesk
**Author:** Steven / SIG Software
**Status:** Draft v0.1
**Last updated:** 2026-06-23

---

## 1. Problem Statement

Salon reception handles two intertwined workloads through one phone line: support-style enquiries (rescheduling, complaints, product questions) and appointment booking (new bookings, treatment-specific scheduling constraints). Odoo Helpdesk solves the first well but has no native concept of therapist/room/duration-based scheduling. Generic booking tools have no ticket/SLA lifecycle. Neither solves the "customer rings, AI books the slot with zero human involvement" requirement.

Goal: a single cross-platform system, modelled around an `Enquiry → Booking` lifecycle rather than a bolted-together helpdesk + calendar, deployable on Windows, macOS, Android and iOS from one .NET MAUI codebase, with an automated voice-booking channel via n8n.

## 2. Goals

- Replace manual phone-call booking with a system that supports both human reception staff and an AI voice agent through the *same* booking engine — no duplicated conflict logic.
- Support beauty-industry-specific scheduling realities: variable treatment duration, staff-unattended processing gaps (e.g. colour), required resources (chair/room), patch-test gating, deposits.
- Single source of truth API; MAUI clients (desktop-first) and n8n webhooks both consume it — never write to SQL directly from the client or from n8n.
- Full audit trail from phone call → transcript → enquiry → booking, for dispute resolution and liability (patch test, allergic reaction claims).

## 3. Non-Goals (v1)

- Full Odoo Helpdesk feature parity (SLA escalation matrices, multi-team routing) — only the subset relevant to a single-site salon.
- In-call payment capture via voice agent (Twilio Pay / SMS payment link is phase 2).
- Multi-site / franchise support (data model should not actively block it, but no UI/admin work for it in v1).
- Customer self-service mobile app (mobile MAUI targets are staff-only in v1: agenda view, push notifications, arrived/completed status).

## 4. Users

| Role | Primary surface | Key needs |
|---|---|---|
| Receptionist | Windows desktop | Fast call-time booking entry, live day-grid by resource, no double-book risk |
| Therapist/Stylist | Android/iPhone | Today's agenda, mark arrived/completed/no-show, push alerts for new/changed bookings |
| Owner/Manager | Windows/Mac desktop | Reporting, SLA-style enquiry resolution tracking, no-show/deposit stats |
| Customer (indirect) | Phone only | Rings in, speaks to AI voice agent or human, gets SMS confirmation |

## 5. Domain Model

```
Customer
  Id, Name, Phone (E.164, unique), Email, Notes,
  ConsentMarketing, ConsentSMS, PreferredTherapistId (nullable),
  PatchTestExpiry (nullable)

Enquiry                         // the "ticket"
  Id, CustomerId, Channel (Phone | N8nVoice | WalkIn | Web),
  Status (New | Triaged | Booked | Resolved | Lost),
  InboundCallSid (nullable), AssignedToUserId, CreatedUtc,
  Tags, TranscriptText (nullable)

Service
  Id, Name, DurationMinutes, Price, RequiresPatchTest,
  BufferBeforeMin, BufferAfterMin, RequiredSkillTag

Staff
  Id, Name, WorkingHoursJson, SkillTags[], MaxConcurrentBookings

Resource                        // chair / room / basin
  Id, Name, Type

Booking
  Id, EnquiryId (nullable), CustomerId, ServiceId, StaffId, ResourceId,
  Status (Tentative | Confirmed | Arrived | Completed | NoShow | Cancelled),
  DepositRequired, DepositPaid, DepositTakenVia,
  RemindersSentJson

BookingSegment                  // see Section 6 — handles unattended processing gaps
  Id, BookingId, StartUtc, EndUtc, StaffOccupied (bool), ResourceOccupied (bool)

CallLog                         // raw telephony audit, separate retention policy (GDPR)
  Id, CallSid, FromNumber, RecordingUrl, DurationSec,
  N8nWorkflowExecutionId, RawTranscriptJson
```

### 5.1 Why `BookingSegment` exists
A colour service is not one contiguous staff-occupied block: apply (15 min, staff busy) → develop (35 min, staff free, customer occupies chair only) → finish (10 min, staff busy again). Modelling `Booking` as a single start/end span per StaffId produces false double-booking conflicts and blocks therapists from legitimately taking a second customer during the gap. Conflict checks run against `BookingSegment.StaffOccupied`/`ResourceOccupied`, not against the parent `Booking` span.

## 6. Functional Requirements

### 6.1 Booking engine (API)
- FR1: Given a service + date range, return available slots accounting for staff working hours, skill tags, existing segments, and resource availability.
- FR2: Slot reservation must be transactional and race-safe — concurrent booking attempts (desktop receptionist vs. voice agent) on the same slot must not both succeed. Use row-locking (`UPDLOCK, HOLDLOCK`) or optimistic concurrency (rowversion) at the segment level.
- FR3: Services flagged `RequiresPatchTest` cannot auto-confirm without a valid `PatchTestExpiry` on the customer record — must be created as `Tentative` and routed to human confirmation.
- FR4: All times stored and transacted in UTC; UI layers convert for display only.

### 6.2 Enquiry/ticket lifecycle
- FR5: Every inbound channel (phone, voice-bot, walk-in, web) creates an `Enquiry` first; an `Enquiry` may produce zero, one, or multiple `Booking`s (e.g. cut + colour + brows from one call).
- FR6: Enquiries the voice agent cannot resolve (unclear intent, complaint, repeat failure) must escalate: flag the Enquiry, push a live alert to reception (SignalR), optionally trigger a Twilio warm transfer to a human line.

### 6.3 MAUI clients
- FR7: Windows/Mac desktop: resource-column day-grid (therapist × time), drag-resize booking creation/editing, optimized for sub-5-second entry during a live phone call.
- FR8: Android/iPhone: read-focused agenda view for the logged-in staff member's day, arrived/completed/no-show status toggles, push notifications on new/changed/cancelled bookings.
- FR9: All clients are thin — no direct SQL Server access from any device; all reads/writes go through the API.

### 6.4 N8n / voice automation
- FR10: Inbound call → Twilio number → Vapi/Retell voice agent → function-calling tool calls hit n8n webhooks → n8n calls the same API endpoints used by the MAUI clients (`GetAvailability`, `IdentifyOrCreateCustomer`, `CreateBooking`, `EscalateToHuman`). No booking logic is duplicated inside n8n.
- FR11: Every voice-originated `Enquiry`/`Booking` stores `InboundCallSid` and a link to the call recording for audit/dispute purposes.
- FR12: On successful booking via any channel, the API triggers an SMS confirmation (Twilio) — mandatory due to speech-recognition error risk on names/dates.

## 7. Non-Functional Requirements

- NFR1: API must enforce its own conflict/patch-test/validation rules regardless of caller (MAUI client or n8n) — single source of truth, no client-side trust.
- NFR2: All n8n→API calls authenticated via API key + HMAC-signed payload, not open webhooks.
- NFR3: Push notification setup (APNs for iOS, FCM for Android) budgeted as a discrete, non-trivial workstream — historically the most fragile part of MAUI cross-platform delivery.
- NFR4: GDPR: `CallLog` (raw recordings/transcripts) retained under a separate, shorter retention policy than `Enquiry`/`Booking` records; customer consent fields (`ConsentMarketing`, `ConsentSMS`) enforced before any marketing SMS/email send.
- NFR5: No naive local-time storage anywhere in the data model — UTC only, to avoid DST corruption (UK BST transitions) and to not block future multi-site expansion.

## 8. Architecture Overview

```
SIG.BeautyDesk.Core    — domain model, no MAUI/EF references
SIG.BeautyDesk.Data     — EF Core, SQL Server
SIG.BeautyDesk.Api      — ASP.NET Core minimal API; consumed by MAUI clients AND n8n webhooks
SIG.BeautyDesk.Maui     — MVVM (CommunityToolkit.Mvvm), thin client, SkiaSharp-based day-grid control
```

Real-time: SignalR pushed from the API to connected desktop clients for live calendar updates and escalation alerts.

## 9. Build Sequencing

1. API + data model + conflict-resolution logic, proven via Swagger/Postman before any UI work.
2. Windows desktop client (majority of bookings happen here on day one).
3. n8n + Vapi/Retell voice integration against the already-proven API.
4. macOS client.
5. Android/iPhone staff agenda + push notifications.

## 10. Open Questions

- Which voice platform: Vapi vs Retell — pricing/latency comparison needed before commit.
- Deposit collection mechanism for phase 2 (Twilio Pay vs SMS payment link vs Stripe Checkout link).
- Single-site only for v1 — confirm no near-term second-site requirement before finalising tenant/site fields in the schema.
- Confirm SMS provider (Twilio assumed throughout — consistent with the voice channel).

## 11. Risks / Technical Debt Flags

- Voice-bot webhook handlers must never re-implement slot-conflict logic independently of the API — guaranteed drift risk if they do.
- Reusing existing Swashbuckle/dependency setup from other SIG Software services without isolating versions risks propagating known assembly mismatches into a fresh codebase.
- `BookingSegment` model must be designed before any UI work begins — retrofitting it after the desktop grid is built will be expensive.
