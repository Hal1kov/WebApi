using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Potok.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Potok.DataFolder
{
    public class DBContextClass
    {

        public static PotokDbContext CreateContext()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(
                    "appsettings.json",
                    optional: false,
                    reloadOnChange: false)
                .Build();

            string? connectionString =
                configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "Строка подключения к базе данных не найдена.");

            var options =
                new DbContextOptionsBuilder<PotokDbContext>()
                    .UseNpgsql(connectionString)
                    .Options;

            return new PotokDbContext(options);
        }
    }
}