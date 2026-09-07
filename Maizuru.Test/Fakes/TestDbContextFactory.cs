// SPDX-LicenseCopyrightText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using Maizuru.Contexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Maizuru.Test.Fakes
{
    public class TestDbContextFactory : IDbContextFactory<MaizuruContext>, IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<MaizuruContext> _options;

        public TestDbContextFactory()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<MaizuruContext>()
                .UseSqlite(_connection)
                .Options;

            using MaizuruContext initialContext = new(_options);
            initialContext.Database.EnsureCreated();
        }

        public MaizuruContext CreateDbContext()
        {
            return new MaizuruContext(_options);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
