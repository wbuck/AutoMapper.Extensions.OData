using AutoMapper.OData.Cosmos.Tests.Entities;

namespace AutoMapper.OData.Cosmos.Tests.Data;

public static class DatabaseSeeder
{
    public static ICollection<Forest> GenerateData() 
    {
        var forest1 = Guid.NewGuid();
        var forest2 = Guid.NewGuid();

        return new List<Forest>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ForestId = forest1,
                Name = "Forest1",
                FakeType = new() { FirstName = "Blair" },
                AdObjects = new List<AdObject>
                {
                    new()
                    {
                        //Id = Guid.NewGuid(),
                        DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(12)),
                        Dc = new()
                       {
                           Id = Guid.NewGuid(),
                           ForestId = forest1,
                           Fqdn = "dc1.contoso.com",
                           FsmoRoles = new List<FsmoRole> { FsmoRole.PdcEmulator, FsmoRole.DomainNamingMaster },
                           Backups = new List<Backup>
                           {
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest1,
                                   DateCreated = DateTime.Now,
                                   Path = "/path/to/dc1/backup1.vhdx"
                               },
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest1,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(10)),
                                   Path = "/path/to/dc1/backup2.vhdx"
                               }
                           },
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "dnsHostName",
                                   Value = "dc1.contoso.com",
                                   FakeComplex = new() { FirstName = "Complex test1" }
                               },
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "operatingSystem",
                                   Value = "Windows 2019",
                                   FakeComplex = new() { FirstName = "Complex test2" }
                               }
                           }
                       }
                    },
                    new()
                    {
                        //Id = Guid.NewGuid(),
                        DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(15)),
                        Dc = new()
                        {
                           Id = Guid.NewGuid(),
                           ForestId = forest1,
                           Fqdn = "dc2.contoso.com",
                           FsmoRoles = new List<FsmoRole> { FsmoRole.RidMaster, FsmoRole.InfrastructureMaster },
                           Backups = new List<Backup>
                           {
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest1,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(5)),
                                   Path = "/path/to/dc2/backup1.vhdx"
                               },
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest1,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(1)),
                                   Path = "/path/to/dc2/backup2.vhdx"
                               }
                           },
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "dnsHostName",
                                   Value = "dc2.contoso.com",
                                   FakeComplex = new() { FirstName = "Complex tes3" }
                               },
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "operatingSystem",
                                   Value = "Windows 2016",
                                   FakeComplex = new() { FirstName = "Complex test4" }
                               }
                           }
                       }
                    },
                    new()
                    {
                       //Id = Guid.NewGuid(),
                       DateAdded = DateTime.Now,
                       Dc = new()
                       {
                           Id = Guid.NewGuid(),
                           ForestId = forest1,
                           Fqdn = "dc3.contoso.com",
                           Backups = new List<Backup>
                           {
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest1,
                                   DateCreated = DateTime.Now,
                                   Path = "/path/to/dc3/backup1.vhdx"
                               }
                           },
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "dnsHostName",
                                   Value = "dc3.contoso.com",
                                   FakeComplex = new() { FirstName = "Complex test5" }
                               },
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "operatingSystem",
                                   Value = "Windows 2012R2",
                                   FakeComplex = new() { FirstName = "Complex test6" }
                               }
                           }
                       }
                    },
                    new()
                    {
                        //Id = Guid.NewGuid(),
                       DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(20)),
                       Dc = new()
                       {
                           Id = Guid.NewGuid(),
                           ForestId = forest1,
                           Fqdn = "dc4.contoso.com",
                           FsmoRoles = new List<FsmoRole> { FsmoRole.SchemaMaster },
                           Backups = new List<Backup>
                           {
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest1,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(5)),
                                   Path = "/path/to/dc4/backup1.vhdx"
                               },
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest1,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(1)),
                                   Path = "/path/to/dc4/backup2.vhdx"
                               }
                           },
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "dnsHostName",
                                   Value = "dc4.contoso.com",
                                   FakeComplex = new() { FirstName = "Complex test7" }
                               },
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "sAMAccountName",
                                   Value = "DC4",
                                   FakeComplex = new() { FirstName = "Complex test8" }
                               }
                           }
                       }
                    },
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Forest2",
                ForestId = forest2,
                FakeType = new() { FirstName = "Piper" },
                AdObjects = new List<AdObject>
                {
                    new()
                    {
                        //Id = Guid.NewGuid(),
                       DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(50)),
                       Dc = new()
                       {
                           Id = Guid.NewGuid(),
                           ForestId = forest2,
                           Fqdn = "dc1.google.com",
                           Backups = new List<Backup>
                           {
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest2,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(25)),
                                   Path = "/path/to/dc1/backup1.vhdx"
                               }
                           },                           
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {
                                   Name = "dnsHostName",
                                   Value = "dc1.google.com"
                               },
                               new()
                               {
                                   Name = "operatingSystem",
                                   Value = "Windows 2022"
                               }
                           }
                       }
                    },
                    new()
                    {
                        //Id = Guid.NewGuid(),
                       DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(15)),
                       Dc = new()
                       {
                           Id = Guid.NewGuid(),
                           ForestId = forest2,
                           Fqdn = "dc2.google.com",
                           FsmoRoles = new List<FsmoRole>
                           {
                               FsmoRole.PdcEmulator,
                               FsmoRole.SchemaMaster,
                               FsmoRole.InfrastructureMaster,
                               FsmoRole.DomainNamingMaster
                           },
                           Backups = new List<Backup>
                           {
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest2,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(10)),
                                   Path = "/path/to/dc2/backup1.vhdx"
                               },
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest2,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(5)),
                                   Path = "/path/to/dc2/backup2.vhdx"
                               },
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest2,
                                   DateCreated = DateTime.Now,
                                   Path = "/path/to/dc2/backup3.vhdx"
                               }
                           },
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {
                                   Name = "dnsHostName",
                                   Value = "dc2.google.com"
                               },
                               new()
                               {
                                   Name = "operatingSystem",
                                   Value = "Windows 2022"
                               }
                           }
                       }
                    },
                    new()
                    {
                        //Id = Guid.NewGuid(),
                       DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(1)),
                       Dc = new()
                       {
                           Id = Guid.NewGuid(),
                           ForestId = forest2,
                           Fqdn = "dc3.google.com",
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "dnsHostName",
                                   Value = "dc3.google.com",
                                   FakeComplex = new() { FirstName = "Complex test9" }
                               },
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "operatingSystem",
                                   Value = "Windows 2022",
                                   FakeComplex = new() { FirstName = "Complex test10" }
                               }
                           }
                       }
                    },
                    new()
                    {
                        //Id = Guid.NewGuid(),
                       DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(20)),
                       Dc = new()
                       {
                           Id = Guid.NewGuid(),
                           ForestId = forest2,
                           Fqdn = "dc4.google.com",
                           FsmoRoles = new List<FsmoRole> { FsmoRole.RidMaster },
                           Backups = new List<Backup>
                           {
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest2,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(5)),
                                   Path = "/path/to/dc4/backup1.vhdx"
                               },
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest2,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(1)),
                                   Path = "/path/to/dc4/backup2.vhdx"
                               }
                           },
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "dnsHostName",
                                   Value = "dc4.google.com",
                                   FakeComplex = new() { FirstName = "Complex test11" }
                               },
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "operatingSystem",
                                   Value = "Windows 2008",
                                   FakeComplex = new() { FirstName = "Complex test12" }
                               }
                           }
                       }
                    },
                }
            },
        };
}
}
