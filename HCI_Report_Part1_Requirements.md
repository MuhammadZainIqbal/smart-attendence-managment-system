# Present! - A Multi-Tenant Attendance Management System
## HCI Project Report — Phase 1
### Section (A): Requirements

**Student Name:** [YOUR_NAME_HERE]  
**Student ID / Roll No.:** [YOUR_ID_HERE]  
**Class / Section:** [YOUR_CLASS_SECTION_HERE]  
**Department / Program:** [YOUR_DEPARTMENT_HERE]  

**Submitted To:** Mr. Usman Ghani  
**Submission Date:** January 6, 2026  

---

## 1. Project Context

**Present!** is a **Multi-Tenant SaaS Attendance Management System** where each **Institute is isolated** to ensure privacy, separation of records, and controlled access.

**Key Innovations (Core Interaction Drivers):**
- **Time-Lock Engine:** Attendance marking and submission are restricted to a defined time window.
- **JWT-protected AJAX/Fetch:** Zero-page-refresh interactions for fast, continuous workflows.
- **Bi-Directional Auto-Enrollment:** Enrollment flows support large student populations and maintain consistent mappings between courses/sections and students.

**Interface & Feedback Aesthetics:**
- Professional **Bootstrap 5** user interface design.
- **Loading spinners** to communicate system state during asynchronous operations.
- **Toast notifications** to confirm successful actions and provide clear, non-intrusive feedback.

---

## 2. Identify Primary Users

The system is designed for three primary user roles, each with distinct goals and responsibilities.

### 2.1 Admins
**Primary goal:** Manage the “handshake” between institutes and users.  
Admins establish the institute boundary, onboard users, and ensure that access is granted correctly inside the institute’s data scope.

### 2.2 Teachers
**Primary goal:** Execute attendance marking within the Time-Lock window.  
Teachers require an uninterrupted marking workflow that supports fast entry, immediate confirmation, and accurate time-bound submission.

### 2.3 Students
**Primary goal:** Monitor attendance percentage to avoid exam debarment.  
Students need clear, accessible visibility of attendance metrics and early awareness of short-attendance risk.

---

## 3. Functional & Non-Functional Requirements

### 3.1 Functional Requirements

- **FR-01: Multi-tenant login authentication**  
  The system shall authenticate users in a way that enforces institute-level tenancy boundaries and prevents cross-institute access.

- **FR-02: Time-restricted attendance submission (Time-Lock Engine)**  
  The system shall allow attendance submission only within a configured attendance window and block late/early submissions.

- **FR-03: Real-time AJAX filtering for reports**  
  The system shall provide report filtering and retrieval using AJAX/Fetch interactions so users can refine results without full page reload.

- **FR-04: Secure JWT-based API calls**  
  The system shall secure asynchronous calls using JWT tokens and validate authorization server-side for every protected operation.

### 3.2 Non-Functional Requirements

- **NFR-01: High usability (mobile-friendly classroom use)**  
  The UI shall be responsive and usable on mobile devices to support real classroom conditions and on-the-go access.

- **NFR-02: Minimal cognitive load (clean dashboards)**  
  The system shall provide clear dashboards and predictable navigation patterns so users can perform tasks with minimal mental effort.

- **NFR-03: Performance (low latency via partial page updates)**  
  The system shall minimize perceived delay by using partial page updates (AJAX/Fetch) for high-frequency interactions such as marking and reporting.

---

## 4. User Needs, Pain Points, and Usage Scenarios

| User | Needs | Pain Points | Scenario |
|------|-------|------------|----------|
| Admin | Bi-directional auto-enrollment for hundreds of students and correct institute scoping | Manual enrollment is repetitive and error-prone; onboarding at scale consumes time | At semester start, the Admin maps courses/sections to students using auto-enrollment while keeping each institute fully isolated |
| Teacher | Fast marking with clear submission feedback inside the Time-Lock window | **Manual register time-waste**; page refreshes interrupt focus and slow down marking | During a lecture, the Teacher opens the class session, marks attendance, and submits within the allowed time window using zero-page-refresh updates |
| Student | Immediate visibility of attendance percentage and shortage risk | Learns about short-attendance too late; unclear summaries reduce awareness | **Checking short-attendance on the go**, the Student uses a mobile view to confirm current percentage and identify urgent risk before exam debarment |

---

## 5. Personas & User Stories

### 5.1 Personas

**Persona 1: Dr. Arshad (Teacher)**
- **Profile:** Busy professor managing multiple sections; comfortable with technology but time-constrained.
- **Goals:** Mark attendance quickly within the Time-Lock window; avoid delays and confusion during class.
- **Frustrations:** Interruptive page reloads, slow systems, unclear confirmation after submission.

**Persona 2: Ali (Student)**
- **Profile:** Undergraduate student balancing classes and commute; often relies on mobile access.
- **Goals:** Track attendance percentage consistently to avoid exam debarment.
- **Frustrations:** Late discovery of short-attendance status; difficulty understanding where the percentage dropped.

### 5.2 User Stories

- As a Teacher, I want the **Time-Lock Engine** to restrict attendance submissions to a defined window, so that attendance remains fair and cannot be altered outside the class period.
- As a Teacher, I want attendance submission to use **JWT-protected AJAX/Fetch** with loading spinners and toasts, so that I can confirm success instantly without any page reload.
- As an Admin, I want **bi-directional auto-enrollment** for courses and sections, so that large student groups can be onboarded quickly with fewer manual errors.
- As a Student, I want **AJAX-driven reports** to show my attendance percentage in real time, so that I can identify short-attendance risk early and avoid exam debarment.

---

[INSERT_IMAGE: USER_JOURNEY_STORYBOARD]
