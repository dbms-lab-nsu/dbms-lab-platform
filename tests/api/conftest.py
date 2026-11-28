import subprocess
from pathlib import Path
from uuid import UUID, uuid4

import pytest


ROOT = Path(__file__).resolve().parents[2]


@pytest.fixture
def learning_db():
    def execute(statement):
        result = subprocess.run(
            [
                "docker", "compose", "exec", "-T", "postgres", "sh", "-c",
                'exec psql -X -qAt -v ON_ERROR_STOP=1 '
                '-U "$POSTGRES_USER" -d "$POSTGRES_DB"',
            ],
            cwd=ROOT,
            input=f"BEGIN;\n{statement}\nCOMMIT;\n",
            text=True,
            capture_output=True,
            timeout=30,
            check=False,
        )
        assert result.returncode == 0, result.stderr
        return result.stdout.strip()

    return execute


@pytest.fixture
def student(registered_user, access_token):
    return {
        "id": str(UUID(registered_user["id"])),
        "headers": {"Authorization": f"Bearer {access_token}"},
    }


@pytest.fixture
def student_factory(api_client, user_data, learning_db):
    def create(role=None):
        assert role in {None, "Author", "Administrator"}
        data = {**user_data, "email": f"progress-{uuid4()}@example.com"}
        registered = api_client.post("/api/auth/register", json=data)
        assert registered.status_code == 201, registered.text
        user_id = str(UUID(registered.json()["id"]))
        if role is not None:
            learning_db(
                'INSERT INTO user_roles ("UserId", "RoleId") '
                f"VALUES ('{user_id}', "
                f'(SELECT "Id" FROM roles WHERE "Name" = \'{role}\'));'
            )
        login = api_client.post(
            "/api/auth/login",
            json={"email": data["email"], "password": data["password"]},
        )
        assert login.status_code == 200, login.text
        headers = {"Authorization": f"Bearer {login.json()['accessToken']}"}
        if role is not None:
            profile = api_client.get("/api/auth/me", headers=headers)
            assert profile.status_code == 200, profile.text
            assert role in profile.json()["roles"]
        return {"id": user_id, "headers": headers}

    return create


@pytest.fixture
def course_factory(learning_db, student_factory):
    author = student_factory(role="Author")

    def create(published=3, status="Published"):
        assert status in {"Published", "Draft", "Archived"}
        assert published >= 0
        course_id, module_id, topic_id = (str(uuid4()) for _ in range(3))
        lessons = [str(uuid4()) for _ in range(published + 1)]
        title = f"Progress course {course_id}"
        statements = [
            'INSERT INTO courses ("Id", "AuthorId", "Title", "Description", "Status") '
            f"VALUES ('{course_id}', '{author['id']}', '{title}', 'API tests', '{status}');",
            'INSERT INTO modules ("Id", "CourseId", "Title", "Description", "SortOrder") '
            f"VALUES ('{module_id}', '{course_id}', 'Module', '', 0);",
            'INSERT INTO topics ("Id", "ModuleId", "Title", "Description", "SortOrder") '
            f"VALUES ('{topic_id}', '{module_id}', 'Topic', '', 0);",
        ]
        for index, lesson_id in enumerate(lessons):
            is_published = "true" if index < published else "false"
            statements.append(
                'INSERT INTO lessons '
                '("Id", "TopicId", "Title", "Content", "SortOrder", "IsPublished") '
                f"VALUES ('{lesson_id}', '{topic_id}', 'Lesson {index}', "
                f"'Test content', {index}, {is_published});"
            )
        learning_db("\n".join(statements))
        return {
            "id": course_id,
            "title": title,
            "lessons": lessons[:-1],
            "hidden": lessons[-1],
            "author": author,
        }

    return create


@pytest.fixture
def course(course_factory):
    return course_factory()
