# Present! - A Multi-Tenant Attendance Management System
## HCI Project Report — Phase 3
### Prototype Development

**Section:** (C)  
**Document Purpose:** This section describes how prototypes for **Present!** evolved from low-fidelity wireframes into a high-fidelity, interactive implementation aligned with modern web application standards.

---

## 1. Wireframes and Interactive Mockups

**Transition from low-fidelity to high-fidelity**
- The prototype development process began with **low-fidelity wireframes** to validate layout, navigation placement, and task sequence without visual detail.
- These wireframes were then translated into a **high-fidelity prototype** (the implemented web application) where real interactions were tested using Bootstrap 5 components and asynchronous Fetch-based updates.

**Wireframe: Attendance Marking Portal (Teacher)**
- The attendance marking wireframe emphasizes speed and clarity:
  - A **clean, scrollable list** of enrolled students.
  - Simple **Present/Absent toggles** per student.
  - A prominent and fixed-position **“Submit”** button to reduce scrolling and ensure completion is always available.
- The structure supports classroom realities where time and attention are limited.

**Wireframe: Student Attendance Dashboard (Student)**
- The student dashboard wireframe is structured for quick understanding followed by details:
  - **Visual summary cards** (e.g., Total Classes, Present Count, Attendance %) at the top.
  - A **detailed history table** below for reviewing individual sessions and attendance outcomes.
- This design reduces the effort needed to answer “Am I safe from short-attendance?” while still providing audit-style detail.

[INSERT_IMAGE: WIREFRAME_COLLECTION_GRID]

---

## 2. Reflecting Core Features

**Marking Attendance (Time-Locked)**
- The teacher prototype reflects the **Time-Lock Engine** by ensuring the interface communicates the marking window.
- When the current time is outside the allowed window:
  - Attendance controls appear in a **disabled state**.
  - The submit action is blocked to prevent invalid submissions.
- This makes the system rule visible in the UI and prevents confusion about “why it did not submit.”

**Student Dashboard and Course Reports (AJAX Filters)**
- The prototypes for dashboards and reports reflect realistic usage by supporting complex datasets without breaking flow.
- **AJAX-driven filtering** makes the experience responsive:
  - Filters can be adjusted without a full page reload.
  - Results update immediately, reinforcing the system’s role as a fast operational tool.

---

## 3. Meaningful Mapping between Actions and System Responses

A prototype is considered usable when user actions produce predictable, visible responses that match the user’s mental model of modern digital systems.

**Action → Response pairs implemented in Present!**

- **Action:** Teacher clicks **“Submit Attendance”**  
  **Response:** System shows a **loading spinner**, performs a Fetch request, then displays a success toast such as **“Perfectly Executed”** (or an error toast if validation fails).

- **Action:** Student clicks **“Filter”** on attendance history  
  **Response:** The **table rows update instantly** using AJAX/Fetch, without any page reload or navigation disruption.

**Why the mapping is meaningful**
- Users expect digital actions (submit/filter) to produce immediate feedback.
- Spinners signal “processing,” while toast messages confirm completion without interrupting the workflow.
- Instant updates align with a modern web mental model: *click → system works → results appear*.

[INSERT_IMAGE: ACTION_RESPONSE_MAPPING_DIAGRAM]

---

## 4. Demonstrating Complete Task Flow

The prototype demonstrates an end-to-end attendance marking experience consistent with real classroom usage and system constraints.

**Attendance Marking Flow (Teacher)**

1. **Authentication (Handshake)**
   - The user enters credentials and is authenticated within the institute boundary.

2. **Selection (Valid Session Within Grace Period)**
   - Teacher logs in → selects a schedule/session that is currently valid within the configured marking window (including any grace period).

3. **Action (Marking Students)**
   - The teacher toggles each student’s state between **Present/Absent** using a clean, mobile-friendly list.

4. **Submission (Fetch Request)**
   - The teacher clicks **Submit** to trigger an asynchronous Fetch API request.
   - A **loading spinner** is shown while the request is processed.

5. **Confirmation (Non-Intrusive Feedback)**
   - The system confirms completion via a toast notification, e.g., **“Perfectly Executed”**.
   - Errors are communicated using an error toast, preserving the same flow and minimizing confusion.

[INSERT_IMAGE: TASK_FLOW_STORYBOARD]

---

## 5. Realistic and Intuitive Standards

**High-fidelity, close-to-final implementation**
- The final prototype is not merely a static mockup; it is a live implementation that behaves like a production system.
- It adheres to modern interaction standards through:
  - **Bootstrap 5** styling and layout patterns (cards, tables, buttons) for a professional appearance.
  - **AJAX/Fetch-based partial updates** to eliminate disruptive “white-blink” reloads.
  - Clear feedback mechanisms (spinners and toast notifications) to support trust and usability.

**Alignment with CCP expectations**
- The prototype is designed to be “close to final implementation,” meaning it supports realistic tasks, realistic data flows, and consistent UI standards.

---

## 6. Formatting Notes

- This section maintains an academic and professional tone aligned with the sample report.
- Bold headings and structured lists are used to make prototype rationale easy to scan and evaluate.
