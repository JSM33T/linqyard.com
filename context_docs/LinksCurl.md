# Links API - cURL Examples

This document provides cURL examples for the Links endpoints in the Linqyard API. Per project conventions these examples use the JSON ApiResponse envelope.

## Base URL
```
https://localhost:7001/link
```

## Authentication
All endpoints require a valid JWT. Include it in the Authorization header:
```bash
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

---

## 📚 Get grouped links

Retrieve all links for the current user, grouped by LinkGroup. Links without a group are returned in the Ungrouped bucket.

```bash
curl -X GET "https://localhost:7001/link" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $(uuidgen)"
```

**Expected Response (200 OK)**
```json
{
  "data": {
    "groups": [
      {
        "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        "name": "Work",
        "description": "Useful work links",
        "sequence": 1,
        "isActive": true,
        "links": [
          {
            "id": "11111111-1111-1111-1111-111111111111",
            "name": "Company intranet",
            "url": "https://intranet.example.com",
            "description": "Internal portal",
            "isActive": true,
            "sequence": 10,
            "groupId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            "createdAt": "2025-09-22T12:00:00Z",
            "updatedAt": "2025-09-22T12:00:00Z"
          }
        ]
      }
    ],
    "ungrouped": {
      "id": "00000000-0000-0000-0000-000000000000",
      "name": "Ungrouped",
      "description": null,
      "sequence": 0,
      "isActive": true,
      "links": [
        {
          "id": "22222222-2222-2222-2222-222222222222",
          "name": "Personal blog",
          "url": "https://blog.example.com",
          "description": "My posts",
          "isActive": true,
          "sequence": 0,
          "groupId": null,
          "createdAt": "2025-09-20T09:30:00Z",
          "updatedAt": "2025-09-20T09:30:00Z"
        }
      ]
    }
  },
  "meta": null
}
```

---

## ➕ Create a new link (POST)

Create a new link owned by the current user. Only POST is used for create/update semantics in these examples.

```bash
curl -X POST "https://localhost:7001/link" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $(uuidgen)" \
  -d '{
    "name": "My site",
    "url": "https://example.com",
    "description": "A helpful site",
    "groupId": null,
    "sequence": 5,
    "isActive": true
  }'
```

**Expected Response (200 OK)**
```json
{
  "data": {
    "id": "33333333-3333-3333-3333-333333333333",
    "name": "My site",
    "url": "https://example.com",
    "description": "A helpful site",
    "isActive": true,
    "sequence": 5,
    "groupId": null,
    "createdAt": "2025-09-28T12:00:00Z",
    "updatedAt": "2025-09-28T12:00:00Z"
  },
  "meta": null
}
```

---

## 🔧 Notes & testing tips

- The API uses bearer JWTs; generate or obtain a token through the auth endpoints before testing.
- These examples intentionally only use GET and POST (no PUT/DELETE) per project preference — if you want a POST-based deletion endpoint (e.g. POST /link/delete with { id }), I can add server support and doc examples.
- Windows PowerShell correlation ID example:
```powershell
$CORRELATION_ID = [System.Guid]::NewGuid().ToString()
```

---

If you'd like, I can add POST-based endpoints for delete or update and then provide corresponding cURL examples.

---

## Group management (POST-only examples)

Create a group (any authenticated user):

```bash
curl -X POST "https://localhost:7001/group" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $(uuidgen)" \
  -d '{
    "name": "Work",
    "description": "Useful work links",
    "sequence": 1
  }'
```

Edit a group (owner or admin):

```bash
curl -X POST "https://localhost:7001/group/{groupId}/edit" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $(uuidgen)" \
  -d '{
    "name": "Work Stuff",
    "sequence": 2
  }'
```

Delete a group (owner or admin) — this will unlink its links (set groupId to null):

```bash
curl -X POST "https://localhost:7001/group/{groupId}/delete" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $(uuidgen)"
```

---

## Edit a link (POST-only)

Edit a link's fields (owner or admin):

```bash
curl -X POST "https://localhost:7001/link/{linkId}/edit" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $(uuidgen)" \
  -d '{
    "name": "Updated site",
    "url": "https://updated.example.com",
    "description": "Updated description",
    "groupId": "{groupId}",
    "sequence": 3,
    "isActive": true
  }'
```

**Expected Response (200 OK)**
```json
{
  "data": {
    "id": "33333333-3333-3333-3333-333333333333",
    "name": "Updated site",
    "url": "https://updated.example.com",
    "description": "Updated description",
    "isActive": true,
    "sequence": 3,
    "groupId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "createdAt": "2025-09-20T09:30:00Z",
    "updatedAt": "2025-09-28T14:15:00Z"
  },
  "meta": null
}
```

---

If you'd like I can also add a short Postman collection or example responses for these POST endpoints.

---

## 🔁 Resequence links (POST-only)

Move links between groups (or to ungrouped) and update their sequence values. Body is an array of objects with id, groupId (nullable), and sequence.

```bash
curl -X POST "https://localhost:7001/link/resequence" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $(uuidgen)" \
  -d '[
    { "id": "006cf39a-4668-4ffa-87d4-2f7400bb5a9d", "groupId": "0f39efb7-5374-4ba5-a138-75a2ae9b0bb3", "sequence": 0 },
    { "id": "c0f68268-4521-4147-8102-519aae1a019f", "groupId": null, "sequence": 0 },
    { "id": "fde1d804-bc81-44c5-90b4-f7630a365d8e", "groupId": null, "sequence": 1 },
    { "id": "158759b6-dec4-4e1a-b3cb-1bd806a07b16", "groupId": null, "sequence": 2 }
  ]'
```

**Expected Response (200 OK)**
```json
{

  ---

  ## 🔔 Record link click (analytics)

  When a preview or any client clicks a link, the client should POST a click event to the server so analytics can be recorded. This endpoint is intentionally tiny and accepts the link id in the path and a minimal body.

  Endpoint:


  POST https://localhost:7001/link/{linkId}/click

  Headers:
  - Content-Type: application/json
  - X-Correlation-Id: $(uuidgen)

  Body (example):
  ```json
  {
    "linkId": "33333333-3333-3333-3333-333333333333",
    "fp": "user-fingerprint-from-localstorage",
    "location": {
      "coords": { "latitude": 12.3456, "longitude": -98.7654, "accuracy": 30 }
    }
  }
  ```

  Notes:
  - The client will include a fingerprint stored in localStorage under the key `fp` when available.
  - `location` and `coords` are optional and only sent if the client can obtain geolocation permission.
  - No Authorization header is required for this analytics endpoint (it's fire-and-forget).

  cURL example:
  ```bash
  curl -X POST "https://localhost:7001/link/33333333-3333-3333-3333-333333333333/click" \
    -H "Content-Type: application/json" \
    -H "X-Correlation-Id: $(uuidgen)" \
    -d '{ "linkId": "33333333-3333-3333-3333-333333333333", "fp": "abc123fingerprint" }'
  ```

  **Expected Response (200 OK)**
  ```json
  {
    "data": { "message": "Recorded" },
    "meta": null
  }
  ```
  "data": { "message": "Resequenced" },
  "meta": null
}
```
