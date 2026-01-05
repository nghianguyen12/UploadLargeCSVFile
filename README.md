# UploadLargeCSVFile

# High Level Design

## Overview

This system is designed to **ingest large CSV files** through a Web API, process the data asynchronously, and store it in PostgreSQL. It supports **multiple files**, **parallel processing with background workers**, and **job progress tracking**.

Key components:
- **CSVUpload API**: Accepts CSV uploads, creates import jobs, and enqueues job IDs to RabbitMQ.
- **Worker Service**: Consumes jobs from RabbitMQ, reads CSV files in batches, and inserts rows into PostgreSQL.
- **PostgreSQL**: Stores both imported data and job metadata.
- **RabbitMQ**: Decouples API from workers for asynchronous, scalable processing.
- **Job Tracking API**: Clients can track job progress.

---

## Architecture Diagram (High-Level)

Client (Browser / App)
|
| HTTP POST /upload (multipart CSV)
v
Web API (JobController)
| Save file & create ImportJob in PostgreSQL
| Publish JobId to RabbitMQ
v
RabbitMQ Queue ("jobs")
^
| Multiple workers listening
v
Worker Service
| Read CSV in batches
| Insert into PostgreSQL safely (transactions / batch insert / GUID keys)
| Update ImportJob progress
v
PostgreSQL Database

## Database Design

CREATE TABLE import_job
(
    job_id uuid NOT NULL,
    file_name text COLLATE pg_catalog."default" NOT NULL,
    file_path text COLLATE pg_catalog."default" NOT NULL,
    status text COLLATE pg_catalog."default" NOT NULL,
    total_rows integer,
    processed_rows integer DEFAULT 0,
    failed_rows integer DEFAULT 0,
    last_processed_row integer DEFAULT 0,
    started_at timestamp without time zone,
    completed_at timestamp without time zone,
    created_at timestamp without time zone DEFAULT now(),
    CONSTRAINT import_job_pkey PRIMARY KEY (job_id)
)

CREATE TABLE customer
(
    name text COLLATE pg_catalog."default" NOT NULL,
    email text COLLATE pg_catalog."default",
    age integer,
    created_at timestamp without time zone DEFAULT now(),
    customer_id uuid NOT NULL,
    CONSTRAINT customer_pkey PRIMARY KEY (customer_id),
    CONSTRAINT customer_age_check CHECK (age >= 0)
)

CREATE TABLE import_error
(
    error_id bigint NOT NULL DEFAULT nextval('import_error_error_id_seq'::regclass),
    job_id uuid,
    row_number integer,
    error_message text COLLATE pg_catalog."default",
    raw_data jsonb,
    created_at timestamp without time zone DEFAULT now(),
    CONSTRAINT import_error_pkey PRIMARY KEY (error_id),
    CONSTRAINT import_error_job_id_fkey FOREIGN KEY (job_id)
        REFERENCES public.import_job (job_id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
)


