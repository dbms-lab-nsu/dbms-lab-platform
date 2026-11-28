from concurrent.futures import ThreadPoolExecutor
from datetime import datetime
from threading import Barrier
from uuid import UUID, uuid4

import pytest


@pytest.fixture
def enrolled_course(api_client, student, course):
    enroll(api_client, student, course)
    return course


def enroll(client, user, course):
    response = client.put(
        f"/api/me/enrollments/{course['id']}", headers=user["headers"]
    )
    assert response.status_code == 200, response.text
    result = response.json()
    assert set(result) == {"courseId", "enrolledAt"}
    assert result["courseId"] == course["id"]
    assert datetime.fromisoformat(result["enrolledAt"]).utcoffset() is not None
    return result


def update(client, user, lesson_id, status):
    response = client.put(
        f"/api/me/lessons/{lesson_id}/progress",
        headers=user["headers"],
        json={"status": status},
    )
    assert response.status_code == 200, response.text
    result = response.json()
    assert set(result) == {"lessonId", "status", "updatedAt"}
    assert result["lessonId"] == lesson_id
    assert result["status"] == status
    assert datetime.fromisoformat(result["updatedAt"]).utcoffset() is not None
    return result


def assert_progress(client, user, course, completed, total, percent):
    response = client.get(
        f"/api/me/courses/{course['id']}/progress", headers=user["headers"]
    )
    assert response.status_code == 200, response.text
    assert response.json() == {
        "courseId": course["id"],
        "completedLessons": completed,
        "totalLessons": total,
        "progressPercent": percent,
    }


def saved_status(learning_db, user, lesson_id):
    return learning_db(
        'SELECT "Status" FROM lesson_progress '
        f'WHERE "UserId" = \'{user["id"]}\' AND "LessonId" = \'{UUID(lesson_id)}\';'
    )


def assert_problem(response, status, *, validation=False):
    assert response.status_code == status, response.text
    assert response.headers["content-type"].split(";")[0] == "application/problem+json"
    problem = response.json()
    assert problem["status"] == status
    assert isinstance(problem["title"], str) and problem["title"]
    assert isinstance(problem["traceId"], str) and problem["traceId"]
    if validation:
        errors = problem["errors"]
        assert isinstance(errors, dict) and errors
        for messages in errors.values():
            assert isinstance(messages, list) and messages
            assert all(isinstance(message, str) and message for message in messages)
    else:
        assert problem["instance"] == response.request.url.path


def saved_updated_at(learning_db, user, lesson_id):
    value = learning_db(
        'SELECT "UpdatedAt" FROM lesson_progress '
        f'WHERE "UserId" = \'{user["id"]}\' AND "LessonId" = \'{UUID(lesson_id)}\';'
    )
    return datetime.fromisoformat(value)


def assert_saved_progress(learning_db, user, result):
    status = {"NotStarted": "0", "InProgress": "1", "Completed": "2"}[result["status"]]
    assert saved_status(learning_db, user, result["lessonId"]) == status
    assert saved_updated_at(learning_db, user, result["lessonId"]) == (
        datetime.fromisoformat(result["updatedAt"])
    )


def archive(client, course):
    response = client.post(
        f"/api/courses/{course['id']}/archive", headers=course["author"]["headers"]
    )
    assert response.status_code == 200, response.text
    assert response.json()["id"] == course["id"]
    assert response.json()["status"] == "Archived"


def parallel_requests(request, count=4):
    barrier = Barrier(count, timeout=10)

    def send(index):
        barrier.wait()
        return request(index)

    with ThreadPoolExecutor(max_workers=count) as executor:
        futures = [executor.submit(send, index) for index in range(count)]
        return [future.result(timeout=30) for future in futures]


def test_enrollment_is_idempotent(api_client, student, course, learning_db):
    first = enroll(api_client, student, course)
    assert enroll(api_client, student, course) == first
    assert learning_db(
        'SELECT count(*) FROM course_enrollments '
        f'WHERE "UserId" = \'{student["id"]}\' AND "CourseId" = \'{course["id"]}\';'
    ) == "1"
    assert_progress(api_client, student, course, 0, 3, 0)


@pytest.mark.parametrize("status", ["Draft", "Archived"])
def test_unpublished_course_rejected(api_client, student, course_factory, status):
    course = course_factory(status=status)
    response = api_client.put(
        f"/api/me/enrollments/{course['id']}", headers=student["headers"]
    )
    assert_problem(response, 403)
    listed = api_client.get("/api/me/enrollments", headers=student["headers"])
    assert listed.status_code == 200, listed.text
    assert listed.json() == []


def test_missing_course(api_client, student):
    course_id = uuid4()
    enrollment = api_client.put(
        f"/api/me/enrollments/{course_id}", headers=student["headers"]
    )
    assert_problem(enrollment, 404)
    progress = api_client.get(
        f"/api/me/courses/{course_id}/progress", headers=student["headers"]
    )
    assert_problem(progress, 404)


def test_my_courses_are_sorted_and_private(
    api_client, student, student_factory, course_factory
):
    empty = api_client.get("/api/me/enrollments", headers=student["headers"])
    assert empty.status_code == 200, empty.text
    assert empty.json() == []
    courses = sorted([course_factory(), course_factory()], key=lambda c: c["title"])
    expected = []
    for course in reversed(courses):
        enrollment = enroll(api_client, student, course)
        expected.append({**enrollment, "courseTitle": course["title"]})
    other = student_factory()
    enroll(api_client, other, course_factory())
    response = api_client.get("/api/me/enrollments", headers=student["headers"])
    assert response.status_code == 200, response.text
    assert response.json() == sorted(expected, key=lambda item: item["courseTitle"])


def test_status_transitions(api_client, student, enrolled_course, learning_db):
    lesson_id = enrolled_course["lessons"][0]
    for status, stored, completed, percent in [
        ("NotStarted", "0", 0, 0),
        ("InProgress", "1", 0, 0),
        ("Completed", "2", 1, 33),
        ("NotStarted", "0", 0, 0),
    ]:
        update(api_client, student, lesson_id, status)
        assert saved_status(learning_db, student, lesson_id) == stored
        assert_progress(api_client, student, enrolled_course, completed, 3, percent)


def test_only_published_lessons_count(api_client, student, enrolled_course, learning_db):
    course = enrolled_course
    learning_db(
        'INSERT INTO lesson_progress ("Id", "UserId", "LessonId", "Status", "UpdatedAt") '
        f"VALUES ('{uuid4()}', '{student['id']}', '{course['hidden']}', 2, NOW());"
    )
    assert_progress(api_client, student, course, 0, 3, 0)
    update(api_client, student, course["lessons"][0], "Completed")
    assert_progress(api_client, student, course, 1, 3, 33)
    learning_db(
        'UPDATE lessons SET "IsPublished" = false '
        f'WHERE "Id" = \'{course["lessons"][0]}\';'
    )
    assert_progress(api_client, student, course, 0, 2, 0)


def test_no_published_lessons(api_client, student, course_factory):
    course = course_factory(published=0)
    enroll(api_client, student, course)
    assert_progress(api_client, student, course, 0, 0, 0)


def test_without_jwt(api_client, student, enrolled_course, learning_db):
    course = enrolled_course
    lesson_id = course["lessons"][0]
    update(api_client, student, lesson_id, "InProgress")
    responses = [
        api_client.put(
            f"/api/me/lessons/{lesson_id}/progress", json={"status": "Completed"}
        ),
        api_client.get(f"/api/me/courses/{course['id']}/progress"),
        api_client.get("/api/me/enrollments"),
        api_client.put(f"/api/me/enrollments/{course['id']}"),
    ]
    assert [r.status_code for r in responses] == [401] * 4, [
        (r.status_code, r.text) for r in responses
    ]
    for response in responses:
        assert response.headers["www-authenticate"].lower().startswith("bearer")
    assert saved_status(learning_db, student, lesson_id) == "1"
    assert_progress(api_client, student, course, 0, 3, 0)


def test_without_enrollment(api_client, student, course, learning_db):
    lesson_id = course["lessons"][0]
    response = api_client.put(
        f"/api/me/lessons/{lesson_id}/progress",
        headers=student["headers"],
        json={"status": "Completed"},
    )
    assert_problem(response, 403)
    assert saved_status(learning_db, student, lesson_id) == ""
    progress = api_client.get(
        f"/api/me/courses/{course['id']}/progress", headers=student["headers"]
    )
    assert_problem(progress, 403)


def test_missing_lesson(api_client, student, enrolled_course):
    response = api_client.put(
        f"/api/me/lessons/{uuid4()}/progress",
        headers=student["headers"],
        json={"status": "Completed"},
    )
    assert_problem(response, 404)
    assert_progress(api_client, student, enrolled_course, 0, 3, 0)


@pytest.mark.parametrize(
    "payload",
    [
        pytest.param({"status": "Unknown"}, id="unknown"),
        pytest.param({"status": -1}, id="negative"),
        pytest.param({"status": 999}, id="out-of-range"),
        pytest.param({"status": None}, id="null"),
        pytest.param({}, id="missing"),
        pytest.param({"status": 0}, id="numeric-not-started"),
        pytest.param({"status": 1}, id="numeric-in-progress"),
        pytest.param({"status": 2}, id="numeric-completed"),
        pytest.param({"status": "2"}, id="numeric-string"),
        pytest.param({"status": True}, id="boolean"),
        pytest.param({"status": []}, id="array"),
        pytest.param(
            {"status": "Completed", "userId": str(uuid4())}, id="extra-user-id"
        ),
    ],
)
def test_invalid_status_preserves_progress(
    api_client, student, enrolled_course, learning_db, payload
):
    lesson_id = enrolled_course["lessons"][0]
    before = update(api_client, student, lesson_id, "InProgress")
    response = api_client.put(
        f"/api/me/lessons/{lesson_id}/progress",
        headers=student["headers"],
        json=payload,
    )
    assert_problem(response, 400, validation=True)
    assert_saved_progress(learning_db, student, before)
    assert_progress(api_client, student, enrolled_course, 0, 3, 0)


def test_two_users_are_isolated(
    api_client, student, student_factory, enrolled_course, learning_db
):
    other = student_factory()
    enroll(api_client, other, enrolled_course)
    first, second = enrolled_course["lessons"][:2]
    update(api_client, student, first, "Completed")
    assert_progress(api_client, other, enrolled_course, 0, 3, 0)
    assert saved_status(learning_db, other, first) == ""
    update(api_client, other, second, "Completed")
    update(api_client, student, second, "Completed")
    assert_progress(api_client, student, enrolled_course, 2, 3, 67)
    assert_progress(api_client, other, enrolled_course, 1, 3, 33)
    update(api_client, student, second, "NotStarted")
    assert_progress(api_client, other, enrolled_course, 1, 3, 33)
    assert saved_status(learning_db, other, second) == "2"


def test_parallel_enrollment(api_client, student, course, learning_db):
    responses = parallel_requests(
        lambda _: api_client.put(
            f"/api/me/enrollments/{course['id']}", headers=student["headers"]
        )
    )
    assert [r.status_code for r in responses] == [200] * 4, [
        (r.status_code, r.text) for r in responses
    ]
    assert all(r.json() == responses[0].json() for r in responses)
    assert learning_db(
        'SELECT count(*) FROM course_enrollments '
        f'WHERE "UserId" = \'{student["id"]}\' AND "CourseId" = \'{course["id"]}\';'
    ) == "1"


@pytest.mark.parametrize("existing", [False, True], ids=["insert", "update"])
def test_parallel_same_lesson(
    api_client, student, enrolled_course, learning_db, existing
):
    lesson_id = enrolled_course["lessons"][0]
    if existing:
        update(api_client, student, lesson_id, "NotStarted")
    responses = parallel_requests(
        lambda _: api_client.put(
            f"/api/me/lessons/{lesson_id}/progress",
            headers=student["headers"],
            json={"status": "Completed"},
        )
    )
    assert [r.status_code for r in responses] == [200] * 4, [
        (r.status_code, r.text) for r in responses
    ]
    for response in responses:
        assert response.json()["lessonId"] == lesson_id
        assert response.json()["status"] == "Completed"
        assert response.json() == responses[0].json()
    assert_saved_progress(learning_db, student, responses[0].json())
    assert_progress(api_client, student, enrolled_course, 1, 3, 33)


def test_parallel_different_lessons(api_client, student, enrolled_course, learning_db):
    lessons = enrolled_course["lessons"]
    responses = parallel_requests(
        lambda index: api_client.put(
            f"/api/me/lessons/{lessons[index]}/progress",
            headers=student["headers"],
            json={"status": "Completed"},
        ),
        count=len(lessons),
    )
    assert [r.status_code for r in responses] == [200] * 3, [
        (r.status_code, r.text) for r in responses
    ]
    for lesson_id in lessons:
        assert saved_status(learning_db, student, lesson_id) == "2"
    assert_progress(api_client, student, enrolled_course, 3, 3, 100)


def test_enrollment_and_progress_in_parallel(api_client, student, course, learning_db):
    lesson_id = course["lessons"][0]

    def request(index):
        if index % 2 == 0:
            return api_client.put(
                f"/api/me/enrollments/{course['id']}", headers=student["headers"]
            )
        return api_client.put(
            f"/api/me/lessons/{lesson_id}/progress",
            headers=student["headers"],
            json={"status": "Completed"},
        )

    responses = parallel_requests(request)
    details = [(r.status_code, r.text) for r in responses]
    assert all(responses[i].status_code == 200 for i in [0, 2]), details
    # A progress request may reach the backend before enrollment commits.
    assert all(responses[i].status_code in {200, 403} for i in [1, 3]), details
    completed = any(responses[i].status_code == 200 for i in [1, 3])
    assert saved_status(learning_db, student, lesson_id) == ("2" if completed else "")
    assert_progress(api_client, student, course, int(completed), 3, 33 if completed else 0)
    assert learning_db(
        'SELECT count(*) FROM course_enrollments '
        f'WHERE "UserId" = \'{student["id"]}\' AND "CourseId" = \'{course["id"]}\';'
    ) == "1"
    update(api_client, student, lesson_id, "Completed")
    assert_progress(api_client, student, course, 1, 3, 33)


@pytest.mark.parametrize("status", ["NotStarted", "InProgress", "Completed"])
def test_repeated_status_preserves_updated_at(
    api_client, student, enrolled_course, learning_db, status
):
    lesson_id = enrolled_course["lessons"][0]
    update(api_client, student, lesson_id, status)
    # A fixed historical timestamp makes an accidental rewrite observable without sleeps.
    learning_db(
        'UPDATE lesson_progress SET "UpdatedAt" = \'2001-01-01T00:00:00Z\' '
        f'WHERE "UserId" = \'{student["id"]}\' AND "LessonId" = \'{lesson_id}\';'
    )
    expected_time = datetime.fromisoformat("2001-01-01T00:00:00Z")
    result = update(api_client, student, lesson_id, status)
    assert datetime.fromisoformat(result["updatedAt"]) == expected_time
    assert_saved_progress(learning_db, student, result)
    completed = int(status == "Completed")
    assert_progress(api_client, student, enrolled_course, completed, 3, completed * 33)


@pytest.mark.parametrize(
    ("value", "canonical"),
    [("notstarted", "NotStarted"), ("inprogress", "InProgress"), ("cOmPlEtEd", "Completed")],
)
def test_status_names_are_case_insensitive(
    api_client, student, enrolled_course, learning_db, value, canonical
):
    lesson_id = enrolled_course["lessons"][0]
    response = api_client.put(
        f"/api/me/lessons/{lesson_id}/progress",
        headers=student["headers"],
        json={"status": value},
    )
    assert response.status_code == 200, response.text
    result = response.json()
    assert result["lessonId"] == lesson_id
    assert result["status"] == canonical
    assert datetime.fromisoformat(result["updatedAt"]).utcoffset() is not None
    assert_saved_progress(learning_db, student, result)
    completed = int(canonical == "Completed")
    assert_progress(api_client, student, enrolled_course, completed, 3, completed * 33)


def test_hidden_lesson_progress_is_forbidden(
    api_client, student, enrolled_course, learning_db
):
    lesson_id = enrolled_course["hidden"]
    response = api_client.put(
        f"/api/me/lessons/{lesson_id}/progress",
        headers=student["headers"],
        json={"status": "Completed"},
    )
    assert_problem(response, 403)
    assert saved_status(learning_db, student, lesson_id) == ""
    assert_progress(api_client, student, enrolled_course, 0, 3, 0)


def test_archived_course_preserves_history(
    api_client, student, enrolled_course, learning_db
):
    course = enrolled_course
    enrollment = enroll(api_client, student, course)
    before = update(api_client, student, course["lessons"][0], "Completed")
    archive(api_client, course)

    rejected = api_client.put(
        f"/api/me/enrollments/{course['id']}", headers=student["headers"]
    )
    assert_problem(rejected, 403)
    rejected = api_client.put(
        f"/api/me/lessons/{course['lessons'][0]}/progress",
        headers=student["headers"],
        json={"status": "NotStarted"},
    )
    assert_problem(rejected, 403)
    assert_saved_progress(learning_db, student, before)
    assert_progress(api_client, student, course, 1, 3, 33)
    listed = api_client.get("/api/me/enrollments", headers=student["headers"])
    assert listed.status_code == 200, listed.text
    assert listed.json() == [{**enrollment, "courseTitle": course["title"]}]


@pytest.mark.parametrize("role", ["Author", "Administrator"])
def test_preview_roles_do_not_bypass_learning_rules(
    api_client, student_factory, course, learning_db, role
):
    user = course["author"] if role == "Author" else student_factory(role=role)
    lesson_id = course["lessons"][0]
    preview = api_client.get(
        f"/api/lessons/{course['hidden']}", headers=user["headers"]
    )
    assert preview.status_code == 200, preview.text

    rejected = api_client.put(
        f"/api/me/lessons/{lesson_id}/progress",
        headers=user["headers"],
        json={"status": "Completed"},
    )
    assert_problem(rejected, 403)
    assert saved_status(learning_db, user, lesson_id) == ""

    enroll(api_client, user, course)
    before = update(api_client, user, lesson_id, "Completed")
    rejected = api_client.put(
        f"/api/me/lessons/{course['hidden']}/progress",
        headers=user["headers"],
        json={"status": "Completed"},
    )
    assert_problem(rejected, 403)
    assert saved_status(learning_db, user, course["hidden"]) == ""

    archive(api_client, course)
    rejected = api_client.put(
        f"/api/me/enrollments/{course['id']}", headers=user["headers"]
    )
    assert_problem(rejected, 403)
    rejected = api_client.put(
        f"/api/me/lessons/{lesson_id}/progress",
        headers=user["headers"],
        json={"status": "NotStarted"},
    )
    assert_problem(rejected, 403)
    assert_saved_progress(learning_db, user, before)
    assert_progress(api_client, user, course, 1, 3, 33)


def test_parallel_different_statuses(
    api_client, student, enrolled_course, learning_db
):
    lesson_id = enrolled_course["lessons"][0]
    statuses = ["NotStarted", "InProgress", "Completed"]
    responses = parallel_requests(
        lambda index: api_client.put(
            f"/api/me/lessons/{lesson_id}/progress",
            headers=student["headers"],
            json={"status": statuses[index]},
        ),
        count=len(statuses),
    )
    assert [r.status_code for r in responses] == [200] * 3, [
        (r.status_code, r.text) for r in responses
    ]
    for status, response in zip(statuses, responses, strict=True):
        assert response.json()["lessonId"] == lesson_id
        assert response.json()["status"] == status
        assert datetime.fromisoformat(response.json()["updatedAt"]).utcoffset() is not None

    stored = saved_status(learning_db, student, lesson_id)
    assert stored in {"0", "1", "2"}
    # Database state determines the winner, not the order of HTTP responses.
    winner = responses[int(stored)].json()
    assert_saved_progress(learning_db, student, winner)
    completed = int(stored == "2")
    assert_progress(api_client, student, enrolled_course, completed, 3, completed * 33)


def test_course_completion_api_regression(
    api_client, student, enrolled_course, registered_user
):
    course = enrolled_course
    response = api_client.get(
        f"/api/courses/{course['id']}/structure", headers=student["headers"]
    )
    assert response.status_code == 200, response.text
    lesson_ids = [
        item["id"]
        for module in response.json()["modules"]
        for topic in module["topics"]
        for item in topic["items"]
    ]
    assert lesson_ids == course["lessons"]
    assert_progress(api_client, student, course, 0, 3, 0)
    for completed, (lesson_id, percent) in enumerate(
        zip(lesson_ids, [33, 67, 100], strict=True), start=1
    ):
        lesson = api_client.get(
            f"/api/lessons/{lesson_id}", headers=student["headers"]
        )
        assert lesson.status_code == 200, lesson.text
        assert lesson.json()["id"] == lesson_id
        assert lesson.json()["courseId"] == course["id"]
        assert lesson.json()["content"] == "Test content"
        update(api_client, student, lesson_id, "InProgress")
        update(api_client, student, lesson_id, "Completed")
        assert_progress(api_client, student, course, completed, 3, percent)
    login = api_client.post(
        "/api/auth/login",
        json={
            "email": registered_user["email"],
            "password": registered_user["password"],
        },
    )
    assert login.status_code == 200, login.text
    returning = {
        **student,
        "headers": {"Authorization": f"Bearer {login.json()['accessToken']}"},
    }
    enroll(api_client, returning, course)
    assert_progress(api_client, returning, course, 3, 3, 100)
