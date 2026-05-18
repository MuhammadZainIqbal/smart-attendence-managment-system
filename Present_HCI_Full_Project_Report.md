# Present! - A Multi-Tenant Attendance Management System

## Human Computer Interaction (HCI) Project Report

### Professional Cover Page

**Project Title:** Present! - A Multi-Tenant Attendance Management System  
**Section:** (A)  

**Submitted By:** [YOUR_NAME_HERE]  
**Roll Number:** [YOUR_ROLL_NUMBER_HERE]  

**Submitted To:** Mr. Usman Ghani  
**Dated:** Jan 2026  

**Department:** Department of Computer Science, UET Lahore (New Campus)

---

## Section 1: Requirements

### 1.1 Project Context

**Present!** is a **Multi-Tenant SaaS Attendance Management System** where each **Institute is isolated** to ensure privacy, separation of records, and controlled access.

**Key Technical Innovations (Core Interaction Drivers)**
- **Time-Lock Engine:** Attendance marking and submission are restricted to a defined time window.
- **JWT-secured AJAX/Fetch (No-Refresh UI):** Zero-page-refresh interactions for fast, continuous workflows.
- **Bi-Directional Auto-Enrollment:** Enrollment flows support large student populations and maintain consistent mappings between courses/sections and students.

**Interface and Feedback Aesthetics**
- Professional **Bootstrap 5** user interface design.
- **Loading spinners** to communicate system state during asynchronous operations.
- **Toast notifications** to confirm successful actions and provide clear, non-intrusive feedback (e.g., “Perfectly Executed”).

### 1.2 Primary Users and Their Goals

The system is designed for three primary user roles, each with distinct goals and responsibilities.

**Admins**
- Primary goal: Manage the “handshake” between institutes and users.
- Admins establish institute boundaries, onboard users, and ensure correct access inside the institute’s data scope.

**Teachers**
- Primary goal: Execute attendance marking within the Time-Lock Engine window.
- Teachers require an uninterrupted marking workflow that supports fast entry, immediate confirmation, and accurate time-bound submission.

**Students**
- Primary goal: Monitor attendance percentage to avoid exam debarment.
- Students need clear, accessible visibility of attendance metrics and early awareness of short-attendance risk.

### 1.3 Functional Requirements

- **FR-01: Multi-tenant login authentication**  
  The system shall authenticate users in a way that enforces institute-level tenancy boundaries and prevents cross-institute access.

- **FR-02: Time-restricted attendance submission (Time-Lock Engine)**  
  The system shall allow attendance submission only within a configured attendance window and block late/early submissions.

- **FR-03: Real-time AJAX filtering for reports**  
  The system shall provide report filtering and retrieval using AJAX/Fetch interactions so users can refine results without full page reload.

- **FR-04: Secure JWT-based API calls**  
  The system shall secure asynchronous calls using JWT tokens and validate authorization server-side for every protected operation.

### 1.4 Non-Functional Requirements

- **NFR-01: High usability (mobile-friendly classroom use)**  
  The UI shall be responsive and usable on mobile devices to support real classroom conditions and on-the-go access.

- **NFR-02: Minimal cognitive load (clean dashboards)**  
  The system shall provide clear dashboards and predictable navigation patterns so users can perform tasks with minimal mental effort.

- **NFR-03: Performance (low latency via partial page updates)**  
  The system shall minimize perceived delay by using partial page updates (AJAX/Fetch) for high-frequency interactions such as marking and reporting.

### 1.5 User Needs, Pain Points, and Usage Scenarios

| User | Needs | Pain Points | Scenario |
|------|-------|------------|----------|
| Admin | Bi-directional auto-enrollment for hundreds of students and correct institute scoping | Manual enrollment is repetitive and error-prone; onboarding at scale consumes time | At semester start, the Admin maps courses/sections to students using auto-enrollment while keeping each institute fully isolated |
| Teacher | Fast marking with clear submission feedback inside the Time-Lock Engine window | Manual register time-waste; page refreshes interrupt focus and slow down marking | During a lecture, the Teacher opens the class session, marks attendance, and submits within the allowed time window using zero-page-refresh updates |
| Student | Immediate visibility of attendance percentage and shortage risk | Learns about short-attendance too late; unclear summaries reduce awareness | Checking short-attendance on the go, the Student uses a mobile view to confirm current percentage and identify urgent risk before exam debarment |

### 1.6 Personas

**Persona 1: Dr. Arshad (Teacher)**
- Profile: Busy professor managing multiple sections; comfortable with technology but time-constrained.
- Goals: Mark attendance quickly within the Time-Lock Engine window; avoid delays and confusion during class.
- Frustrations: Interruptive page reloads, slow systems, unclear confirmation after submission.

**Persona 2: Ali (Student)**
- Profile: Undergraduate student balancing classes and commute; often relies on mobile access.
- Goals: Track attendance percentage consistently to avoid exam debarment.
- Frustrations: Late discovery of short-attendance status; difficulty understanding where the percentage dropped.

### 1.7 User Stories

- As a Teacher, I want the **Time-Lock Engine** to restrict attendance submissions to a defined window, so that attendance remains fair and cannot be altered outside the class period.
- As a Teacher, I want attendance submission to use **JWT-secured AJAX/Fetch** with loading spinners and toasts, so that I can confirm success instantly without any page reload.
- As an Admin, I want **bi-directional auto-enrollment** for courses and sections, so that large student groups can be onboarded quickly with fewer manual errors.
- As a Student, I want **AJAX-driven reports** to show my attendance percentage in real time, so that I can identify short-attendance risk early and avoid exam debarment.

[INSERT_IMAGE: PRIMARY_USER_INTERACTION_STORYBOARD]

---

## Section 2: Interface Design & Usability Principles

This section explains how the interface design supports fast, low-friction attendance workflows under realistic classroom constraints.

### 2.1 Interface Layout, Navigation Clarity, and Screen Flow

- **Dashboard-centric layout**
  - The interface is designed around a sidebar-based dashboard that acts as the central hub for each role.
  - Primary tasks (attendance marking, reporting, monitoring) are accessible via predictable dashboard entry points.

- **Fixed sidebar for constant navigation access**
  - A fixed sidebar remains visible to provide constant orientation and reduce navigation uncertainty.

- **AJAX/Fetch “No-Refresh” flow**
  - The interaction pattern prioritizes a no “white blink” experience by using AJAX/Fetch-based updates for key actions.
  - Typical flow:
    - Login → Dashboard → AJAX-driven action (e.g., filtering a report) → instant UI update
  - This preserves context and improves perceived performance during repeated tasks.

[INSERT_IMAGE: INTERFACE_LAYOUT_SCREEN_FLOW]

### 2.2 Application of Usability Heuristics

- **Visibility of system status**
  - During JWT-secured API calls (Fetch requests), loading spinners provide immediate confirmation that the system is processing.

- **Feedback (action confirmation)**
  - After key actions such as attendance submission, toast notifications confirm completion professionally (e.g., “Perfectly Executed”).

- **Consistency and standards**
  - Standard Bootstrap 5 components (cards, modals, tables, buttons) are used consistently so users do not need to learn new patterns.

### 2.3 Cognitive and Physical Load Considerations

- **Cognitive load**
  - Summary cards (e.g., total students, attendance %) present key metrics at a glance, reducing mental math.

- **Physical load**
  - AJAX/Fetch reduces physical load by eliminating repeated full page reloads and re-navigation.
  - Mobile-first marking screens use large tap targets to reduce mis-taps under time pressure.

### 2.4 Color, Typography, and Iconography

- **Color**
  - High-contrast status colors convey meaning instantly: green for Present and red for Absent.

- **Typography**
  - Noto Sans supports readability and clear hierarchy between headings and body text.

- **Iconography**
  - Standard icons are paired with labels to improve recognition without creating ambiguity.

### 2.5 Accessibility Design

- **Contrast and readability**
  - High-contrast colors and legible typography support clarity in bright classroom environments.

- **Inclusive design under real constraints**
  - Responsive layout enables teachers to mark attendance on low-end mobile devices.
  - Lightweight AJAX payloads improve usability in areas with weak or unstable campus Wi‑Fi.

---

## Section 3: Prototype Development

This section documents the transition from wireframes to an interactive, high-fidelity prototype that reflects real system behaviors.

### 3.1 Wireframes to High-Fidelity Prototype

- The design process began with **low-fidelity wireframes** to validate layout, navigation placement, and task sequence.
- The final prototype is a **high-fidelity implementation** that uses Bootstrap 5 components and AJAX/Fetch updates to mimic realistic usage.

**Wireframe focus: Attendance Marking Portal (Teacher)**
- Clean list of students with Present/Absent toggles and a prominent submit action.

**Wireframe focus: Student Dashboard (Student)**
- Summary cards at the top with a detailed history table below to balance quick insight with traceable detail.

[INSERT_IMAGE: WIREFRAME_GRID_COLLECTION]

### 3.2 Reflecting Core Features

- **Time-Lock Engine behavior in the UI**
  - When outside the allowed window, the marking interface shows disabled controls and blocks submission.
  - This makes the system rule visible and prevents invalid submissions.

- **AJAX-driven filtering for reports**
  - Report screens support complex datasets through asynchronous filtering so the interface feels responsive and realistic.

### 3.3 Action–Response Mapping

A meaningful prototype ensures that user actions produce system responses that match the user’s mental model of instant digital interaction.

- **Action:** Teacher clicks “Submit Attendance”  
  **Response:** A spinner appears, the secure Fetch request runs, and the system confirms with a toast (e.g., “Perfectly Executed”).

- **Action:** Student filters attendance history  
  **Response:** Table content updates instantly without page reload.

[INSERT_IMAGE: ACTION_RESPONSE_MAPPING_DIAGRAM]

### 3.4 Complete Attendance Marking Task Flow

1. **Authentication (Handshake):** Teacher enters credentials and is authenticated within the institute boundary.
2. **Selection:** Teacher selects a valid schedule/session from the dashboard within the Time-Lock Engine window (including grace period).
3. **Marking:** Teacher toggles Present/Absent for students.
4. **Submission:** Teacher clicks submit to trigger the Fetch request.
5. **Confirmation:** The system confirms with a non-intrusive toast message (e.g., “Perfectly Executed”).

### 3.5 Realistic and Intuitive Standards

- The prototype adheres to modern standards through Bootstrap 5 styling, predictable component behavior, and no-refresh UI updates.
- The result is visually professional and operationally close to final implementation expectations.

---

## Section 4: Evaluation & Testing

This section reports how Present! was evaluated using multiple HCI methods and how feedback informed iterative improvements.

### 4.1 Selection of Evaluation Methods

- **Questionnaire (Google Forms style)**
  - Goal: Gather quantitative user satisfaction data using a SUS-style scale.

- **Usability testing (classroom observation)**
  - Goal: Observe teacher workflows and measure time-on-task for attendance marking in realistic conditions.

- **Heuristic review (expert check)**
  - Goal: Compare the interface against Nielsen’s 10 usability heuristics, with emphasis on **Visibility of System Status**.

### 4.2 Findings (Issues, Severity, Fix)

| Issue | Severity | Fix / Improvement |
|------|----------|-------------------|
| “White blink” full page refresh during marking felt like a slow system response | High | Replaced standard form submissions with AJAX/Fetch updates to preserve context and avoid disruptive reloads |
| Lack of immediate feedback after clicking “Submit” | Medium | Added loading spinners during Fetch and success/error toast notifications (e.g., “Perfectly Executed”) |
| Students found it hard to visualize overall attendance record | Medium | Added pie charts and summary cards to show percentage instantly without digging through tables |

### 4.3 Comparison of Methods (Lab vs. Real-World)

| Testing Type | Primary Focus | Strengths | Typical Findings in Present! |
|-------------|---------------|-----------|------------------------------|
| Lab Testing | Finding technical bugs and UI alignment | Controlled conditions make issues reproducible; useful for visual consistency checks | Form validation issues, layout inconsistencies, and predictable interaction defects |
| Real-World Environment Testing | Teacher behavior in a noisy classroom and the effect of unstable campus Wi‑Fi on system performance | Captures realistic time pressure, interruptions, device constraints, and network instability | Perceived slowness due to reloads (“white blink”), stress due to short Time-Lock window, and the importance of lightweight asynchronous updates |

### 4.4 Iteration History (Evidence of Improvement)

**Iteration 1 (MVP): Basic CRUD + full page reloads**
- Early version relied on standard page reload flows.
- Result: Teachers experienced disruption during marking; system felt slower than expected.

**Iteration 2: AJAX/Fetch + loading spinners**
- Introduced a no-refresh interaction model for high-frequency actions.
- Added spinners to improve visibility of system status and reduce uncertainty.

**Iteration 3: JWT-secured APIs + toast confirmations**
- Secured key operations with JWT-based API calls.
- Added professional success/error toasts (e.g., “Perfectly Executed”) to confirm state changes.

**Time pressure improvement (Time-Lock Engine refinements)**
- Expanded the Time-Lock grace period and introduced a “time remaining” indicator to reduce teacher stress.

[INSERT_IMAGE: USABILITY_DATA_CHART]

---

## Section 5: Final Visuals & Storyboards

This section provides the storyboard narrative for the final visuals used in the report and identifies where final screenshots should be inserted.

### 5.1 Storyboard Description (Teacher Marking Attendance)

**Frame 1 — Context (Teacher in class)**
- A teacher stands at the front of a classroom and opens the Present! dashboard on a smartphone.

**Frame 2 — Interaction (Marking students)**
- The teacher views a list of students with Present/Absent toggles and prepares to submit.

**Frame 3 — System response (Loading spinner / AJAX)**
- After tapping submit, a loading spinner appears while a secure AJAX/Fetch request is processed.

**Frame 4 — Result (Success toast / “Perfectly Executed”)**
- A success toast appears stating “Perfectly Executed,” and the teacher returns attention to the class.

[INSERT_IMAGE: FINAL_STORYBOARD_PANELS]

### 5.2 Actual Site Screenshots (Insert Evidence)

[INSERT_IMAGE: ACTUAL_SITE_SCREENSHOT_DASHBOARD]

[INSERT_IMAGE: ACTUAL_SITE_SCREENSHOT_ATTENDANCE_MARKING]

[INSERT_IMAGE: ACTUAL_SITE_SCREENSHOT_REPORTS]
