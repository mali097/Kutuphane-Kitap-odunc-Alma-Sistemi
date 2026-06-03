-- Suç ve Ceza: test sırasında yanlış girilen kitap bilgilerini düzelt
UPDATE Books
SET
    Title = N'Suç ve Ceza',
    Author = N'Fyodor Dostoyevski',
    Isbn = N'9789750719107',
    Genre = 21,
    PublishYear = 1866,
    PageCount = 671,
    Publisher = N'İş Bankası Kültür',
    UpdatedDate = SYSUTCDATETIME(),
    UpdatedBy = 3
WHERE Id = 1;

-- Seeder ile gelen mükerrer kaydı kaldır (ödünç/puan verileri Id=1 üzerinde)
UPDATE Books
SET
    IsDeleted = 1,
    UpdatedDate = SYSUTCDATETIME(),
    UpdatedBy = 3
WHERE Id = 61;

UPDATE WeeklyRecommendations
SET BookTitle = N'Suç ve Ceza'
WHERE Id = 1;
