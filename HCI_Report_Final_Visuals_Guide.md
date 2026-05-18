# Present! - A Multi-Tenant Attendance Management System
## HCI Report — Final Visuals & Viva Prep Guide (Supplement)

**Purpose:** This document provides (1) storyboard scene descriptions, (2) copy‑paste prompts for generating visuals for the report placeholders (Time‑Lock, AJAX/Fetch, JWT), (3) a viva/presentation strategy focused on interface decisions (not code), and (4) a final assembly checklist for compiling the report in the sample Word document (2023CS608.docx).

---

## 1. Storyboard Scene Descriptions (4-Frame)

### Storyboard: “Teacher Marking Attendance” Flow

**Frame 1 — Context & Start (Dashboard Access)**
- Scene: A teacher stands at the front of a classroom with students seated.
- Action: The teacher unlocks a smartphone and opens the **Present!** dashboard.
- UI detail: A clean dashboard screen with a visible institute-aware header and a “Today’s Schedule” entry.

**Frame 2 — Primary Task (Marking with Toggles)**
- Scene: Close-up view of the phone screen.
- Action: The teacher views a scrollable list of students.
- UI detail: Each student row has a clearly labeled **Present/Absent** toggle (or segmented control). A prominent **Submit** button remains visible.

**Frame 3 — System Processing (Spinner Feedback)**
- Scene: The teacher taps **Submit**.
- Action: The interface shows that it is processing the request.
- UI detail: A **loading spinner** appears near the submit area (or overlay), indicating a JWT-secured Fetch request is in progress. The marking controls are temporarily disabled to prevent duplicate submissions.

**Frame 4 — Completion (Success Toast + Return to Class)**
- Scene: The teacher sees confirmation and resumes teaching.
- Action: A success message appears; the teacher puts the phone away.
- UI detail: A **success toast** appears (top-right style) with the message **“Perfectly Executed”** and a green success indicator, confirming the attendance state was saved.

---

## 2. Image Generation Prompts for AI (lmarena.ai / Midjourney style)

Use these prompts as copy‑paste inputs. Each prompt is written to produce visuals aligned with the system features: **Multi‑Tenant**, **Time‑Lock Engine**, **AJAX/Fetch** (no “white blink”), and **JWT‑secured interactions**.

These prompts are mapped to the placeholders used across Parts 1–4:
- Part 1: `USER_JOURNEY_STORYBOARD`
- Part 3: `WIREFRAME_COLLECTION_GRID`, `TASK_FLOW_STORYBOARD`
- Part 2: `HEURISTIC_FEEDBACK_EXAMPLES`
- Part 4: `BEFORE_AFTER_ITERATION_SCREENSHOTS`

### Prompt 1 (Low-Fidelity Wireframes: WIREFRAME_COLLECTION_GRID)
"Create a professional grid of 3 low-fidelity UI wireframes for a university attendance app called 'Present!'. Layout: a single image showing three wireframe screens side-by-side (or in a 3-tile grid). Screen 1 (Teacher Mobile): attendance marking list with student rows and Present/Absent toggles, prominent Submit button, note 'Time-Lock window active'. Screen 2 (Student Dashboard): top summary cards (Attendance %, Present, Absent, Total) and a pie chart placeholder, with a table section labeled 'History'. Screen 3 (Reports): filter controls (Course, Section, Date) above a results table, labeled 'AJAX filter updates (no page reload)'. Style: monochrome wireframe, rectangles and labels only, no branding logos, clear spacing, legible text." 

### Prompt 2 (User Journey Storyboard: USER_JOURNEY_STORYBOARD / TASK_FLOW_STORYBOARD)
"A hand-drawn 4-panel storyboard (four frames) showing a university professor using a smartphone app called 'Present!' to mark attendance. Frame 1: teacher in class opens the Present! dashboard on phone. Frame 2: close-up of student list with Present/Absent toggles and a visible Submit button. Frame 3: teacher taps Submit, a loading spinner appears indicating an AJAX/Fetch request. Frame 4: a success toast appears saying 'Perfectly Executed' with a green check, teacher puts phone away and continues teaching. Style: clean storyboard sketch, clear captions under each frame, no copyrighted logos." 

### Prompt 3 (High-Fidelity Dashboard: Student view)
"A modern, clean high-fidelity web dashboard UI for a student in an attendance system called 'Present!'. Style: Bootstrap 5 look and feel, Noto Sans typography, sidebar navigation on the left. Main content: summary cards at the top (Attendance %, Present, Absent, Total Classes) and a pie chart showing Present vs Absent. Below: attendance history table with dates and status chips. Use high-contrast status colors: green for Present, red for Absent. Production-ready spacing, no real university branding, no copyrighted UI." 

### Prompt 4 (AJAX Feedback UI: HEURISTIC_FEEDBACK_EXAMPLES)
"Close-up of a web app UI showing AJAX feedback in an attendance system called 'Present!'. Show a sidebar dashboard layout (Bootstrap 5 style, Noto Sans). The user just clicked 'Submit Attendance': a loading spinner is visible, then a top-right success toast appears with a green checkmark and the message 'Perfectly Executed'. Include subtle UI text like 'Saving...' and 'Saved' to show state transitions. No real tokens displayed; no copyrighted branding." 

### Prompt 5 (Before/After Iteration: BEFORE_AFTER_ITERATION_SCREENSHOTS)
"A side-by-side comparison image with two labeled panels: 'Before (Iteration 1)' and 'After (Iteration 3)'. Before: a basic old web form for attendance marking with full page reload feel, no spinner, no toast, minimal feedback. After: a modern Bootstrap 5 sidebar interface using AJAX/Fetch with a visible loading spinner during submit and a green success toast saying 'Perfectly Executed'. Clear labels calling out 'No white-blink reload' and 'Instant feedback'. Generic fictional UI, no copyrighted design." 

---

## 3. Viva Defense Strategy (HCI Focus)

Below are difficult interface-focused questions with “senior developer” answers grounded in HCI principles.

1) **Question:** Why did you choose a sidebar instead of a top navbar?  
   **Answer:** A sidebar offers persistent navigation on desktop and keeps the content area stable for data-heavy tables and dashboards. This improves orientation, reduces navigation effort, and supports scan-based workflows where the user repeatedly returns to the same actions.

2) **Question:** How did you minimize the physical load for teachers during marking?  
   **Answer:** Physical load was reduced by minimizing repeated navigation and waiting. AJAX/Fetch avoids full page reloads and keeps the teacher in the same context; large tap targets and a stable list layout reduce mis-taps and repetitive correction, especially on mobile during classroom use.

3) **Question:** How did you handle error recovery in a way that supports classroom time pressure?  
   **Answer:** Error recovery is addressed through immediate, non-blocking feedback and safe retry behavior. If a submit fails, the UI shows an error toast with a clear next action (retry), preserves the marked states locally on-screen, and avoids forcing the user to re-enter data after a network interruption.

4) **Question:** Why use toast notifications instead of modal pop-ups for confirmation?  
   **Answer:** Toasts confirm state changes without blocking the user, which is essential for rapid marking. Modals introduce interruption cost and extra clicks; toasts provide acknowledgment while preserving flow and reducing cognitive switching.

5) **Question:** How do you justify the time feedback (Time-Lock + time remaining) from a usability standpoint?  
   **Answer:** Time constraints create stress unless the system makes them visible. A time remaining indicator reduces uncertainty, supports planning, and prevents last-minute failure. Disabling submission outside the window also prevents errors by constraining the user to valid actions.

---

## 4. Final Assembly Checklist (Markdown → Word / 2023CS608.docx)

Use this checklist to compile the report cleanly and consistently in the sample document format.

- **Step 1:** Open `2023CS608.docx` (sample format) and create/locate the sections where you will paste each part.

- **Step 2:** Open the four report parts and confirm headings are consistent:
  - HCI_Report_Part1_Requirements.md
  - HCI_Report_Part2_Design_Principles.md
  - HCI_Report_Part3_Prototype_Development.md
  - HCI_Report_Part4_Evaluation_Testing.md

- **Step 3:** Paste sections into the Word document in order (Part 1 → Part 4), using the same heading hierarchy as the sample.

- **Step 4:** Replace image placeholders with visuals:
  - Insert the generated images where placeholders appear (e.g., WIREFRAME_COLLECTION_GRID, TASK_FLOW_STORYBOARD, etc.).

- **Step 5:** Verify style consistency:
  - Ensure a consistent font (prefer **Noto Sans** if available; otherwise use **Arial** consistently).
   - Keep heading sizes consistent with the sample (Title vs section headers vs body text).
   - Verify image placement and captions do not break page flow.
   - Verify status colors remain high-contrast (green for Present, red for Absent) and readable when printed.

- **Step 6:** Final quality pass:
  - Check table formatting in Word (column widths, header shading optional, readable spacing).
  - Ensure bullets are aligned and not over-indented.
  - Confirm terminology consistency: Time-Lock Engine, AJAX/Fetch, JWT-secured APIs, Multi-Tenant SaaS.
