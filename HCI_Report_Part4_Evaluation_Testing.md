# Present! - A Multi-Tenant Attendance Management System
## HCI Project Report — Phase 4
### Evaluation & Testing

**Section:** (D)  
**Document Purpose:** This section documents how **Present!** was evaluated using structured HCI methods, what usability problems were discovered, and how iterative improvements were applied to reach a more stable, classroom-ready experience.

---

## 1. Selection of Evaluation Methods

**Present!** was evaluated using a mixed-method approach to capture both quantitative satisfaction data and real task performance outcomes.

**1.1 Questionnaire (Google Forms style)**
- Purpose: Collect quantitative user feedback on satisfaction, learnability, and perceived speed.
- Instrument: A SUS-style scale (System Usability Scale) to summarize usability perception in a comparable format.
- Outcome type: Ratings and trends indicating what users found easy vs. frustrating.

**1.2 Usability Testing (Classroom observation)**
- Purpose: Observe real behavior and measure teacher efficiency under classroom constraints.
- Measurement focus: **Time-on-Task** for teachers marking attendance (from opening the schedule to successful submission).
- Outcome type: Practical evidence of friction points (e.g., interruptions, delays, confusion, stress triggers).

**1.3 Heuristic Review (Expert check)**
- Purpose: Evaluate the interface against **Nielsen’s 10 Usability Heuristics**.
- Emphasis: **Visibility of System Status**, because attendance marking is time-critical and users require immediate feedback.
- Outcome type: Design-level issues and improvement opportunities independent of user familiarity.

---

## 2. Reporting Findings with Clarity

The evaluation revealed usability issues primarily related to workflow continuity, feedback clarity, and time pressure.

### Identified Issues (with Severity and Fixes)

- **Issue:** “White-blink” full page refresh during marking felt like a slow system response.  
  **Severity:** High  
  **Fix Applied:** Replaced standard Form POST flows with **AJAX/Fetch** updates to avoid disruptive reloads and maintain context.

- **Issue:** Lack of immediate feedback after clicking “Submit.”  
  **Severity:** Medium  
  **Fix Applied:** Added **loading spinners** during Fetch calls and **success/error Toast notifications** to confirm completion (e.g., “Perfectly Executed”).

- **Issue:** Students found it hard to visualize their overall attendance record.  
  **Severity:** Medium  
  **Fix Applied:** Added **Pie Charts** and **Summary Cards** to present attendance percentage instantly without requiring table-based interpretation.

### Key Findings Summary (Observed Patterns)

- Teachers described full-page reloads as distracting during time-sensitive marking.
- The **Time-Lock window** was initially too short (e.g., 5 minutes), increasing teacher stress and error risk.
- Students wanted quicker access to attendance percentage without digging through detailed history tables.

---

## 3. Comparison of Methods Used

The following comparison summarizes how lab-style testing and real-world environment testing contributed different insights.

| Testing Type | Primary Focus | Strengths | Typical Findings in Present! |
|-------------|---------------|-----------|------------------------------|
| Lab Testing | Finding technical bugs and UI alignment | Controlled conditions make issues reproducible; useful for visual consistency checks | Form validation issues, layout inconsistencies, and predictable interaction defects |
| Real-World Environment Testing | Teacher behavior in a noisy classroom and the effect of unstable campus Wi‑Fi on system performance | Captures realistic time pressure, interruptions, device constraints, and network instability | Perceived slowness due to reloads (“white blink”), stress due to short Time-Lock window, and the importance of lightweight asynchronous updates |

---

## 4. Evidence of Improvement based on Feedback (Iteration)

The evaluation results were used to guide iterative refinements. Each iteration targeted a specific cluster of usability problems.

### Iteration 1: Initial MVP
- Characteristics: Basic CRUD workflows and standard full-page reload interactions.
- Limitations observed:
  - Teachers experienced workflow disruption during marking due to page reloads.
  - Feedback after submissions was not sufficiently immediate.

### Iteration 2: AJAX/Fetch + Loading Spinners (System Feedback Improvements)
- Change introduced: Replaced standard form submissions with **AJAX/Fetch** to support smooth UI transitions.
- Feedback improvement: Added **loading spinners** to improve visibility of system status.
- Resulting benefit: Reduced perceived delay and eliminated disruptive “white blink” transitions.

### Iteration 3: JWT-Secured APIs + Toast Confirmation
- Change introduced: Integrated **JWT-secured API calls** for protected operations.
- Feedback improvement: Added professional toast confirmations such as **“Perfectly Executed”** for successful state changes.
- Resulting benefit: Improved trust and clarity—users could confidently confirm that attendance changes were recorded.

### Additional Iteration Improvements (Time Pressure Reduction)
- Expanded the Time-Lock grace period to reduce teacher stress.
- Added a **“Time Remaining” indicator** to make the marking window visible and prevent last-minute uncertainty.

---

## 5. Formatting Notes

- This section maintains an academic and professional tone consistent with earlier phases.
- Bold headings and clean tables are used to keep evaluation outcomes clear and scannable.

[INSERT_IMAGE: USABILITY_TESTING_DATA_CHART]

[INSERT_IMAGE: BEFORE_AFTER_ITERATION_SCREENSHOTS]
