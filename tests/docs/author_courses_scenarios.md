# Author course management scenarios

## Scope and sources

These scenarios cover the five endpoints specified in the QA task:

- POST /api/courses
- PATCH /api/courses/{id}
- GET /api/author/courses
- POST /api/courses/{id}/publish
- POST /api/courses/{id}/archive

Requirement sources:

- Technical specification 1.0.3, section 3.4.1: course creation.
- Technical specification 1.0.3, section 3.4.2: course editing.
- Technical specification 1.0.3, section 3.4.4: course archiving.
- QA task: author course listing, publishing, access checks, and API automation.

The local backend currently defines the Course entity and its three states,
but does not implement these endpoints. All scenarios are Not run.
Missing implementation is not recorded here as a reproduced defect.

## Confirmed business rules

- Creating a course requires an authenticated user with the Author role.
- A newly created course has the Draft state.
- Invalid creation data must not result in a saved course.
- Editing requires an existing course owned by the authenticated Author.
- Invalid editing data must not change the saved course.
- Archiving requires an existing course owned by the authenticated Author.
- Draft and Published courses can transition to Archived.
- An Archived course cannot be archived again.
- The author list must contain courses belonging to the current author.

Publication validation and its permitted source states require clarification.
Do not infer these rules from enum names or from the endpoint name.

## Contract decisions required before automation

| ID | Decision |
| --- | --- |
| C01 | Define create and patch request fields, types, required fields, length limits, whitespace handling, null handling, and unknown-field behavior. |
| C02 | Define exact success HTTP codes, response bodies, identifiers, and state serialization for all five endpoints. |
| C03 | Define exact error codes and JSON schemas for missing JWT, insufficient role, foreign ownership, missing course, invalid input, and invalid transition. |
| C04 | Define publication requirements, including whether modules, topics, or lessons are required. |
| C05 | Define publication source states, repeated publication behavior, and whether Archived courses can be published. |
| C06 | Define whether Published and Archived courses can be edited and whether editing can change their state. |
| C07 | Define list schema, pagination, filters, ordering, and visibility of Draft, Published, and Archived courses. |
| C08 | Agree on a supported way to provision two Author users and obtain their JWTs. Registration currently creates Students only. |
| C09 | Define the API read operation used to inspect persisted course details; the author list may be used if it exposes all required fields. |
| C10 | Define test data retention or an authorized cleanup mechanism. Deletion is outside this endpoint scope. |

The endpoint paths alone do not settle these decisions.
Do not accept several alternative HTTP codes merely to make a test pass.

## Test data and isolation

- Author A and Author B are different users with independently obtained JWTs.
- Student S is a registered user without the Author role.
- Anonymous requests omit the Authorization header.
- Create unique users and course names for each test using UUID-based values.
- Obtain user and course identifiers from API responses, not fixed UUIDs.
- Create each scenario's courses independently; do not depend on test order.
- Prepare Published and Archived courses through supported API transitions.
- A generated missing-course UUID must be confirmed absent through the agreed API.
- Do not forge Author claims as a substitute for supported role provisioning.
- Record retained test data and cleanup behavior in the run instructions.

Existing pytest fixtures api_client, registered_user, and access_token can be
reused for HTTP access and Student authentication. Author provisioning and
course factories will be added after the relevant contracts are agreed.

## Scenario matrix

Each row is a scenario group. Separate parameter values become separate tests.
For every response, assert the agreed exact HTTP code and applicable JSON schema.
For rejected mutations of an existing course, reread it as its owner and verify
that its fields, ownership, and state have not changed.

| ID | Setup and action | Expected result | Source / dependency |
| --- | --- | --- | --- |
| AC01 | Author A creates a course with valid data. | A course is created with a generated ID, owner A, and state Draft; a separate read returns the submitted fields. | 3.4.1; C01, C02, C09 |
| AC02 | Anonymous user submits a valid create request. | Access is denied; no course is created. | 3.4.1; C03 |
| AC03 | Student S submits a valid create request. | Access is denied; no course is created. | 3.4.1; C03 |
| AC04 | Author A creates a course with each agreed invalid field value. | Validation fails and no course is saved. | 3.4.1; C01, C03 |
| AC05 | Author A edits an owned Draft course with valid data. | Submitted changes persist; omitted fields and ownership remain unchanged. | 3.4.2; C01, C02, C09 |
| AC06 | Author B attempts to edit Author A's course. | Access is denied and the course is unchanged. | 3.4.2; C03 |
| AC07 | Author A edits an owned course with each agreed invalid value. | Validation fails and the previous data remains unchanged. | 3.4.2; C01, C03 |
| AC08 | Author A patches a confirmed nonexistent course ID. | A missing-resource error is returned; no course is created. | QA task; C03 |
| AC09 | Authors A and B each own courses; each requests the author list. | Each result includes that author's expected courses and excludes the other author's courses across all relevant pages. | QA task; C07 |
| AC10 | A new Author with no courses requests the author list. | An empty collection is returned using the agreed response schema. | QA task; C02, C07 |
| AC11 | Author A publishes an owned course satisfying all publication requirements. | The course becomes Published and a separate read confirms the state. | QA task; C04, C05, C09 |
| AC12 | Author B attempts to publish Author A's course. | Access is denied and the course is unchanged. | QA task; C03 |
| AC13 | Author A publishes a course missing each required publication prerequisite. | Publication is rejected and the course remains unchanged. | QA task; C04 |
| AC14 | Author A archives an owned Draft course. | The state becomes Archived and a separate read confirms it. | 3.4.4; C02, C09 |
| AC15 | Author A archives an owned Published course. | The state becomes Archived and a separate read confirms it. | 3.4.4; C02, C09 |
| AC16 | Author A archives an owned Archived course again. | The transition is rejected and the course remains unchanged. | 3.4.4; C03 |
| AC17 | Author B attempts to archive Author A's course. | Access is denied and the course is unchanged. | 3.4.4; C03 |
| AC18 | Anonymous and Student users attempt patch, author list, publish, and archive separately. | Access is denied; attempted mutations do not change the target course. | QA task; C03 |
| AC19 | Author A attempts publish and archive with confirmed nonexistent IDs. | Each action returns the agreed missing-resource error. | QA task; C03 |
| AC20 | Author A attempts each forbidden publication transition. | Each transition is rejected and the course remains unchanged. | QA task; C05 |
| AC21 | Author A edits owned Published and Archived courses separately. | Each request follows the agreed state-specific editing policy. | Contract clarification; C06 |

## Verification and reporting

Separate API reads verify observable persistence, not direct database contents.
The absence of a course after rejected creation requires an agreed observation
method; do not treat the rejection response alone as proof of database state.

Use pytest and httpx, consistent with the existing API suite.
Add execution instructions and JUnit XML reporting with the automated tests.
Report passed, failed, skipped, and setup-error results from an actual run only.
Until execution is authorized and the backend is available, report Not run.
For reproduced defects, prepare Redmine entries with the scenario ID,
environment, request, expected result, actual result, and reproduction steps.
Remove JWTs, passwords, and other secrets from attached evidence.
