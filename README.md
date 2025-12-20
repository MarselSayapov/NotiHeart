# NotiHeart MVP (Docker Compose)

## Run

```bash
docker compose -f compose.yaml up --build
```

## Smoke test

1. Send a notification with an attachment:

```bash
curl -X POST "http://localhost:5000/api/notifications/send" \
  -H "Accept: application/json" \
  -F "channel=Email" \
  -F "recipient=user@example.com" \
  -F "text=Hello" \
  -F "attachments=@./sample.pdf;type=application/pdf"
```

2. Use the returned `notificationId` to check status and attempts:

```bash
curl -X GET "http://localhost:5000/api/notifications/{notificationId}"
```

When the Email worker processes the message, the status should become `Sent` and at least one attempt will be visible in the response.
