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



## Database Design


