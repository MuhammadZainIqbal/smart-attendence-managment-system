# Present! - A Multi-Tenant Attendance Management System
## HCI Project Report — Phase 2
### Design Principles

**Section:** (B)  
**Document Purpose:** This section documents the interface design principles and HCI rationale applied to the dashboard experience of **Present!**, a multi-tenant attendance management system.

---

## 1. Interface Layout, Navigation Clarity, and Screen Flow

- **Dashboard-centric layout**
  - The interface is designed around a sidebar-based dashboard that acts as the system’s central hub for each role (Admin, Teacher, Student).
  - Primary tasks (attendance marking, reporting, monitoring) are surfaced through consistent dashboard entry points.

- **Fixed sidebar for constant navigation access**
  - A fixed sidebar remains visible to provide constant orientation and reduce navigation uncertainty.
  - This supports quick switching between high-frequency screens without forcing users to “hunt” for menus.

- **No-refresh (AJAX-driven) screen flow**
  - The interaction pattern prioritizes “no white-blink reloads” by using AJAX/Fetch-based updates for key actions.
  - Typical flow:
    - Login → Dashboard → AJAX-driven Action (e.g., filtering a report) → Instant UI Update
  - This reduces disruptions and maintains user context (the user stays on the same page while content updates).

[INSERT_IMAGE: INTERFACE_LAYOUT_FLOW]

---

## 2. Application of Usability Heuristics

- **Visibility of system status**
  - During JWT-secured API calls (Fetch requests), **CSS loading spinners** provide immediate visual confirmation that the system is processing.
  - This reduces uncertainty by signaling that the system is “thinking” rather than unresponsive.

- **Feedback (action confirmation)**
  - After key actions such as attendance submission, **Bootstrap 5 Toast notifications** appear (e.g., “Perfectly Executed”).
  - Toasts confirm success/failure without interrupting the workflow, supporting continuous task completion.

- **Consistency and standards**
  - The interface uses standard **Bootstrap 5** components (e.g., Cards, Modals) to maintain familiar patterns.
  - Consistent component usage reduces the learning curve because users do not need to learn custom UI behaviors.

[INSERT_IMAGE: HEURISTIC_FEEDBACK_EXAMPLES]

---

## 3. Cognitive & Physical Load Considerations

- **Cognitive load reduction**
  - Summary Cards (e.g., Total Students, % Attendance) present key metrics at a glance.
  - This design minimizes mental computation (e.g., manual percentage calculation) and supports quicker decisions.

- **Physical load reduction**
  - AJAX/Fetch updates reduce physical effort by eliminating repeated full page reloads and the need to re-navigate to the same screen.
  - The attendance marking screen is designed for mobile-first classroom use with **large tap targets**, supporting faster input and fewer mis-taps.

---

## 4. Appropriateness of Color, Typography, and Iconography

- **Color (status meaning at a glance)**
  - High-contrast status colors communicate attendance state instantly:
    - **Success/Green** for Present
    - **Danger/Red** for Absent
  - This supports rapid recognition under time pressure (e.g., marking attendance during class).

- **Typography (readability and hierarchy)**
  - The UI uses **Noto Sans** typography to support clarity and comfortable reading across devices.
  - Clear hierarchy is maintained through consistent use of headings (e.g., H1/H2) and body text (p tags) for scannability.

- **Icons (recognition over recall)**
  - Standard icons are used alongside text labels to improve recognition and reduce interpretation time.
  - Icons are not used alone; text labels remain present to prevent ambiguity.

---

## 5. Accessibility Design

- **Color contrast and readability**
  - High-contrast color usage improves readability and supports users working in bright classroom environments.
  - Clear spacing and legible typography improve comprehension for quick, repeated tasks.

- **Inclusive design under real constraints**
  - The responsive layout supports teachers marking attendance on low-end mobile devices.
  - The system’s AJAX-driven approach reduces bandwidth usage due to lightweight payloads, making the experience more reliable in areas with weak Wi‑Fi.

---

## 6. Formatting Notes

- This section uses a professional, academic tone aligned with the sample report style.
- Bullet points are used to improve readability and support quick scanning of design rationale.
