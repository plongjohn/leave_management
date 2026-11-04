-- Development database setup for Leave Management (see ADR 0005).
--
-- Run once as the PostgreSQL superuser:
--   psql -U postgres -f database/setup-dev.sql
-- The password for the role leave_app is asked for interactively, or can be passed with
--   -v app_password=...
-- The script can be run again safely; it then only updates the password.
-- The schema is created by EF Core migrations, not by this script.

\set ON_ERROR_STOP on

\if :{?app_password}
\else
\prompt 'Password for the role leave_app: ' app_password
\endif

-- CREATEDB is needed by the integration tests, which create one database per test run.
SELECT format('CREATE ROLE leave_app LOGIN CREATEDB PASSWORD %L', :'app_password')
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'leave_app')
\gexec

SELECT format('ALTER ROLE leave_app LOGIN CREATEDB PASSWORD %L', :'app_password')
\gexec

SELECT 'CREATE DATABASE leave_management_dev OWNER leave_app'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'leave_management_dev')
\gexec

\echo 'Done: role leave_app and database leave_management_dev are ready.'
