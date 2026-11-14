# DocuMind Frontend ↔ Backend API Contract

This document details the interface contract expected by the React 19 frontend and implemented by the ASP.NET Core .NET 10 backend.

---

## 1. Network & Origin Configuration

* **Backend Base URL**: `http://localhost:5000` (configurable in frontend via `VITE_API_BASE_URL`)
* **Allowed Frontend Origins**:
  * `http://localhost:5173` (Vite dev server)
  * `http://localhost:3000`
  * `http://127.0.0.1:5173`
* **Allowed HTTP Methods**: `GET`, `POST`, `DELETE`, `OPTIONS`
* **Allowed Request Headers**: `Content-Type`, `Accept`, `Authorization`

---

## 2. Endpoints

### 2.1. System Health

#### `GET /health` / `GET /api/health`
Used by orchestrators, health checkers, and frontend status bars.

* **Response Status**: `200 OK`
* **Response Body**:
  ```json
  {
    "status": "Healthy",
    "service": "DocuMind.WebApi",
    "version": "1.0.0",
    "framework": ".NET 10.0",
    "timestamp": "2026-11-13T22:45:00Z"
  }
  ```

---

### 2.2. Documents (`frontend/src/services/documentService.js`)

#### `GET /api/documents`
Lists all uploaded and indexed documents.

* **Response Status**: `200 OK`
* **Response Body**:
  ```json
  [
    {
      "id": "e4585c5a-2e45-4228-b805-4c07a97ce6e5",
      "name": "Cloud_Computing_Notes.pdf",
      "pages": 18,
      "size": "1.8 MB",
      "status": "Ready",
      "updated": "Today"
    }
  ]
  ```

#### `POST /api/documents`
Uploads a single PDF file (enforces max size: 20 MB).

* **Content-Type**: `multipart/form-data`
* **Form Field**: `file` (Binary PDF stream)
* **Response Status**: `201 Created`
* **Response Body**:
  ```json
  {
    "id": "e4585c5a-2e45-4228-b805-4c07a97ce6e5",
    "name": "Cloud_Computing_Notes.pdf",
    "pages": 18,
    "size": "1.8 MB",
    "status": "Ready",
    "updated": "Just now"
  }
  ```

#### `DELETE /api/documents/{id}`
Deletes document record, storage file, and associated vector chunks.

* **Response Status**: `204 No Content`
* **Response Body**: Empty (handled as `null` by `apiClient.js`)

---

### 2.3. RAG Streaming Chat (`frontend/src/services/chatService.js`)

#### `POST /api/documents/{documentId}/ask/stream`
Asynchronous Server-Sent Events (SSE) stream answering user queries grounded in the specified PDF.

* **Request Headers**:
  * `Content-Type: application/json`
  * `Accept: text/event-stream`
* **Request Body**:
  ```json
  {
    "question": "What are the core conclusions of this paper?"
  }
  ```

* **Response Headers**:
  * `Content-Type: text/event-stream; charset=utf-8`
  * `Cache-Control: no-cache`
  * `Connection: keep-alive`

* **Stream Event Payload Specification**:

Each SSE event line is formatted as: `data: <JSON>\n\n`

1. **Sources Event** (`type: "sources"`):
   ```json
   {
     "type": "sources",
     "sources": [
       {
         "page": 3,
         "snippet": "Our experimental results demonstrate a 24% throughput increase...",
         "score": 0.892
       }
     ]
   }
   ```
2. **Token Event** (`type: "token"`):
   ```json
   {
     "type": "token",
     "content": "The "
   }
   ```
3. **Done Event** (`type: "done"`):
   ```json
   {
     "type": "done",
     "answer": ""
   }
   ```

---

## 3. Standard Error Format (RFC 7807)

All non-2xx responses emit `application/problem+json`:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Entity 'Document' with identifier 'e4585c5a-2e45-4228-b805-4c07a97ce6e5' was not found.",
  "instance": "/api/documents/e4585c5a-2e45-4228-b805-4c07a97ce6e5",
  "traceId": "0HN7UBM2A109K:00000001",
  "timestamp": "2026-11-13T22:45:00Z"
}
```
