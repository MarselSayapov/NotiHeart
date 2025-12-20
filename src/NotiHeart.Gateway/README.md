# Notification Gateway MVP

## Multipart send example

```bash
curl -X POST "http://localhost:5000/api/notifications/send" \
  -H "Accept: application/json" \
  -F "channel=Email" \
  -F "recipient=user@example.com" \
  -F "text=Hello from NotiHeart" \
  -F "attachments=@./sample.pdf;type=application/pdf"
```

### Send with explicit correlation id

```bash
curl -X POST "http://localhost:5000/api/notifications/send" \
  -H "Accept: application/json" \
  -F "channel=Sms" \
  -F "recipient=+15555550100" \
  -F "text=SMS content" \
  -F "correlationId=custom-corr-001"
```

### Get notification status and attempts

```bash
curl -X GET "http://localhost:5000/api/notifications/{notificationId}"
```
