BEGIN;

ALTER TABLE UserAccounts ADD COLUMN UserLevel integer NOT NULL DEFAULT 0;
ALTER TABLE UserAccounts ADD COLUMN UserFlags integer NOT NULL DEFAULT 0;
ALTER TABLE UserAccounts ADD COLUMN UserTitle varchar(64) NOT NULL DEFAULT '';

COMMIT;

