-- Add admin user directly (plain password — case-sensitive username)
-- Example:
INSERT INTO AdminUsers (Username, Password, CreatedAt)
VALUES (N'Hassan', N'Qwerty123@', GETUTCDATE());

-- View admins:
-- SELECT Id, Username, Password, CreatedAt FROM AdminUsers;
