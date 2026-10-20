"""Integration check against a running API; removes only its own test record.

Usage: python3 backend/smoke_test.py http://localhost:5000
"""
import json
import sys
from urllib.error import HTTPError
from urllib.request import Request, urlopen

base = (sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5000").rstrip("/")


def call(method, path, body=None, expected=200):
    data = json.dumps(body).encode() if body is not None else None
    req = Request(base + path, data=data, method=method,
                  headers={"Content-Type": "application/json"})
    try:
        response = urlopen(req, timeout=20)
    except HTTPError as error:
        response = error
    with response:
        payload = response.read()
        assert response.status == expected, (method, path, response.status, payload)
        return json.loads(payload) if payload else None


call("GET", "/api/health")
call("POST", "/api/todos", {"title": "   "}, 400)
call("POST", "/api/todos", {"title": "x" * 201}, 400)
item = call("POST", "/api/todos", {"title": "  Kiểm thử công việc  "}, 201)
path = f'/api/todos/{item["id"]}'
try:
    assert item["title"] == "Kiểm thử công việc"
    assert item["isCompleted"] is False
    assert any(row["id"] == item["id"] for row in call("GET", "/api/todos"))
    call("PUT", path, {"title": ""}, 400)
    updated = call("PUT", path, {"title": "Đã sửa", "isCompleted": True})
    assert updated["title"] == "Đã sửa" and updated["isCompleted"] is True
    assert call("GET", path)["isCompleted"] is True
    updated = call("PUT", path, {"title": "Đã sửa", "isCompleted": False})
    assert updated["isCompleted"] is False
finally:
    call("DELETE", path, expected=204)
call("GET", path, expected=404)
call("DELETE", path, expected=404)
call("PUT", path, {"title": "Missing"}, 404)
print("PASS: health, create, list, read, edit, complete/reopen, validation, delete, 404")
