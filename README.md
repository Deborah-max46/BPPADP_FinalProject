# Consumer's Voice System
### ADECOR Rwanda – Consumer Rights Protection Platform

---

## #1. Project Name
**Consumer's Voice System**

---

## #2. Problem Statement

Consumer rights violations in Rwanda are widespread, yet most consumers lack accessible and structured channels to report them. ADECOR Rwanda (Rwanda Consumers' Rights Protection Organization) currently relies on manual, paper-based complaint processes that are fragmented, inconsistent, and difficult to analyze. Consumers must physically visit offices or make telephone calls during business hours, which excludes rural populations and limits complaint volume to what staff can manually handle. There is no real-time case tracking, no automated routing, no SLA monitoring, and no structured data collection that could support evidence-based advocacy or policy development. This results in unresolved complaints, frustrated consumers, and a weakened capacity for ADECOR Rwanda to protect consumer rights at scale.

---

## #3. Objective

To design and develop a web-based Consumer's Voice System for ADECOR Rwanda that enables consumers to submit and track complaints digitally, supports advocates in managing and resolving cases efficiently through workflow automation, and generates data-driven insights to strengthen consumer rights advocacy and policy development in Rwanda.

This system includes features like:
- Guided complaint submission with evidence upload and category selection
- Role-based dashboards for Consumers, Advocates, Administrators, and Business Owners
- Automated complaint routing and SLA deadline tracking
- Geographic heatmaps showing complaint density by district
- Category-wise trend analysis for advocacy reporting
- Anonymous complaint submission for sensitive cases
- Multi-channel notifications via SMS and email
- Business accountability profiles and response interface
- Public education portal on consumer rights
- Anonymized data export for academic research

---

## #4. Functional and Non-Functional Requirements

### Functional Requirements

| # | Requirement |
|---|---|
| FR1 | Consumers can register, log in, and select their role |
| FR2 | Consumers can submit complaints with category, description, evidence, location, and severity |
| FR3 | System detects and flags duplicate complaints automatically |
| FR4 | Complaints are automatically routed to available case officers based on category |
| FR5 | Advocates can view, update, escalate, and resolve assigned cases |
| FR6 | Administrators can manage users, configure workflows, and generate reports |
| FR7 | Businesses can view complaints filed against them and submit responses |
| FR8 | System sends automated SMS and email notifications at each case status change |
| FR9 | Analytics module generates heatmaps, trend charts, and advocacy reports |
| FR10 | System supports anonymous complaint submission for sensitive cases |
| FR11 | All actions are recorded in a tamper-evident audit log |
| FR12 | Anonymized complaint data can be exported for academic research |

### Non-Functional Requirements

| # | Requirement |
|---|---|
| NFR1 | System response time must be under 3 seconds for all primary actions |
| NFR2 | Platform must be accessible on both desktop and mobile browsers |
| NFR3 | All sensitive data must be encrypted end-to-end |
| NFR4 | System must comply with GDPR and Rwanda data protection regulations |
| NFR5 | Role-based access control must prevent unauthorized data access |
| NFR6 | System must support offline form filling with background synchronization |
| NFR7 | Platform must achieve 99.5% uptime during business hours |
| NFR8 | All user passwords must be securely hashed before storage |

---

## #5. Use Case Diagram
```
                    ┌─────────────────────────────────────────┐
                    │         Consumer's Voice System          │
                    └─────────────────────────────────────────┘

  ┌──────────┐      ● Register / Login
  │          │      ● Submit Complaint
  │ Consumer │      ● Upload Evidence
  │          │      ● Track Case Status
  └──────────┘      ● Receive Notifications
                    ● Access Education Portal

  ┌──────────┐      ● View Assigned Cases
  │          │      ● Investigate Complaints
  │ Advocate │      ● Contact Business
  │          │      ● Update Case Status
  └──────────┘      ● Escalate Urgent Cases
                    ● Mark Cases Resolved

  ┌──────────┐      ● Manage All Users
  │          │      ● Assign Cases to Advocates
  │  Admin   │      ● Monitor SLA Compliance
  │          │      ● View Analytics & Heatmaps
  └──────────┘      ● Generate Advocacy Reports
                    ● Configure System Settings

  ┌──────────┐      ● View Complaints Filed Against Them
  │          │      ● Submit Responses
  │ Business │      ● View Compliance Rating
  │          │      ● Update Business Profile
  └──────────┘
```

---

## #6. Database Diagram
```
┌─────────────────────┐         ┌─────────────────────────┐
│        USERS        │         │       COMPLAINTS         │
├─────────────────────┤         ├─────────────────────────┤
│ id (PK)             │──────── │ id (PK)                  │
│ full_name           │    1    │ consumer_id (FK→Users)   │
│ email               │    │   │ title                    │
│ password_hash       │    │   │ description              │
│ role                │    │   │ category                 │
│ region              │    N   │ severity                 │
│ is_anonymous        │         │ status                   │
│ created_at          │         │ location                 │
└─────────────────────┘         │ submitted_at             │
                                │ resolved_at              │
                                └─────────────────────────┘
                                            │
                                            │ 1
                                            │
                                ┌───────────┴─────────────┐
                                │         CASES            │
                                ├─────────────────────────┤
                                │ id (PK)                  │
                                │ complaint_id (FK)        │
                                │ advocate_id (FK→Users)   │
                                │ priority                 │
                                │ sla_deadline             │
                                │ outcome                  │
                                │ assigned_at              │
                                └─────────────────────────┘

┌─────────────────────┐         ┌─────────────────────────┐
│      EVIDENCE       │         │      NOTIFICATIONS       │
├─────────────────────┤         ├─────────────────────────┤
│ id (PK)             │         │ id (PK)                  │
│ complaint_id (FK)   │         │ user_id (FK→Users)       │
│ file_url            │         │ message                  │
│ file_type           │         │ channel (SMS/Email/App)  │
│ uploaded_at         │         │ is_read                  │
└─────────────────────┘         │ sent_at                  │
                                └─────────────────────────┘

┌─────────────────────┐         ┌─────────────────────────┐
│     BUSINESSES      │         │       AUDIT_LOGS         │
├─────────────────────┤         ├─────────────────────────┤
│ id (PK)             │         │ id (PK)                  │
│ owner_id (FK→Users) │         │ user_id (FK→Users)       │
│ business_name       │         │ action                   │
│ registration_no     │         │ target_table             │
│ compliance_rating   │         │ target_id                │
│ response_count      │         │ timestamp                │
└─────────────────────┘         └─────────────────────────┘
```

|

---

## Prototype Test Accounts

| Role | Email | Password |
|---|---|---|
| Consumer | consumer@test.com | password123 |
| Advocate | advocate@test.com | password123 |
| Admin | admin@test.com | password123 |
| Business | business@test.com | password123 |

---

## Author
**DOT NET GROUP F**
ID: 26244 · Uwase Ketsia Deborah
ID: 26134 . Philemon MUVUNYI
ID: 25881 . Ainembabazi Eunice
ID: 25960 . Hirwa Yvan
Adventist University of Central Africa (AUCA)
Bachelor of Science in Information Technology – Software Engineering
March 2026
