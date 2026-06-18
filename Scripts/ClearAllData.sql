-- Clears all business data. Run when you want a fresh catalog.
-- Keeps migration history intact.

DELETE FROM [Reviews];
DELETE FROM [OrderItems];
DELETE FROM [Orders];
DELETE FROM [ToyImages];
DELETE FROM [Toys];
DELETE FROM [ToyCategories];
DELETE FROM [Customers];
DELETE FROM [AdminUsers];
