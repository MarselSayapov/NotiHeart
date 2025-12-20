CREATE TABLE notifications (
    id uuid PRIMARY KEY,
    correlation_id text NOT NULL,
    channel text NOT NULL,
    recipient text NOT NULL,
    text text NOT NULL,
    metadata jsonb NULL,
    status text NOT NULL,
    last_error text NULL,
    created_at timestamptz NOT NULL DEFAULT (now() at time zone 'utc'),
    updated_at timestamptz NOT NULL DEFAULT (now() at time zone 'utc')
);

CREATE INDEX idx_notifications_status ON notifications (status);
CREATE INDEX idx_notifications_created_at ON notifications (created_at);

CREATE TABLE notification_attempts (
    id uuid PRIMARY KEY,
    notification_id uuid NOT NULL REFERENCES notifications(id),
    attempt_no integer NOT NULL,
    result text NOT NULL,
    error text NULL,
    started_at timestamptz NOT NULL DEFAULT (now() at time zone 'utc'),
    finished_at timestamptz NULL
);

CREATE INDEX idx_notification_attempts_notification_id ON notification_attempts (notification_id);

CREATE TABLE notification_attachments (
    id uuid PRIMARY KEY,
    notification_id uuid NOT NULL REFERENCES notifications(id),
    file_name text NOT NULL,
    content_type text NOT NULL,
    content bytea NOT NULL,
    size bigint NOT NULL
);

CREATE INDEX idx_notification_attachments_notification_id ON notification_attachments (notification_id);
