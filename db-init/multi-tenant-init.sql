-- Creates one database per simulated single-tenant instance, plus
-- tenant-router's, all in the same Postgres server/superuser for simplicity.
-- patient-portal has no database of its own — it's a stateless orchestrator
-- that calls tenant-router over HTTP.
-- Runs once, on first container init (docker-entrypoint-initdb.d).
CREATE DATABASE tenant_router;
CREATE DATABASE tenant1;
CREATE DATABASE tenant2;
CREATE DATABASE tenant3;
CREATE DATABASE tenant4;
CREATE DATABASE tenant5;
