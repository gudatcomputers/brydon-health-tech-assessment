-- Creates one database per simulated single-tenant instance, plus
-- patient-portal's, all in the same Postgres server/superuser for simplicity.
-- Runs once, on first container init (docker-entrypoint-initdb.d).
CREATE DATABASE patient_portal;
CREATE DATABASE tenant1;
CREATE DATABASE tenant2;
CREATE DATABASE tenant3;
CREATE DATABASE tenant4;
CREATE DATABASE tenant5;
