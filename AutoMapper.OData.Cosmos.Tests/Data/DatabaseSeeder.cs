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
                FakeType = new()
                {
                    FirstName = "Blair" ,
                    AnotherFakeType = new() { Number = 69 }
                },
                AdObjects = new List<AdObject>
                {
                    new()
                    {
                        DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(12)),
                        FakeObjectOne = new()
                        {
                            Value = Random.Shared.Next(0, 1000),
                            InternalFakeObject = new()
                            {
                                MyValue = $"My number is: {Random.Shared.Next(0, 1000)}"
                            }
                        },
                        FakeObjectTwo = new()
                        {
                            Value = Random.Shared.Next(0, 1000),
                        },
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
                                   Path = "/path/to/dc1/backup1.vhdx",
                                   Credentials = new()
                                   {
                                       Username = "bill@microsoft.com",
                                       Password = "mypassword"
                                   },
                                   NetworkInformation = new()
                                   {
                                       Address = "www.microsoft.com"
                                   }
                               },
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest1,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(10)),
                                   Path = "/path/to/dc1/backup2.vhdx",
                                   Credentials = new()
                                   {
                                       Username = "ted@microsoft.com",
                                       Password = "mypassword1"
                                   },
                                   NetworkInformation = new()
                                   {
                                       Address = "www.microsoft.com"
                                   }
                               }
                           },
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "dnsHostName",
                                   Value = "dc1.contoso.com",
                                   FakeComplex = new() 
                                   { 
                                       FirstName = "Complex test1" ,
                                       AnotherFakeType = new() { Number = Random.Shared.Next(0, 1000) }
                                   }
                               },
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "operatingSystem",
                                   Value = "Windows 2019",
                                   FakeComplex = new() 
                                   { 
                                       FirstName = "Complex test2",
                                       AnotherFakeType = new() { Number = Random.Shared.Next(0, 1000) }
                                   }
                               }
                           }
                       }
                    },
                    new()
                    {
                        DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(15)),
                        FakeObjectOne = new()
                        {
                            Value = Random.Shared.Next(0, 1000),
                            InternalFakeObject = new()
                            {
                                MyValue = $"My number is: {Random.Shared.Next(0, 1000)}"
                            }
                        },
                        FakeObjectTwo = new()
                        {
                            Value = Random.Shared.Next(0, 1000),
                        },
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
                                   Path = "/path/to/dc2/backup1.vhdx",
                                   Credentials = new()
                                   {
                                       Username = "susan@amazon.com",
                                       Password = "mypassword2"
                                   },
                                   NetworkInformation = new()
                                   {
                                       Address = "www.amazon.com"
                                   }
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
                                   FakeComplex = new() 
                                   { 
                                       FirstName = "Complex tes3",
                                       AnotherFakeType = new() { Number = Random.Shared.Next(0, 1000) }
                                   }
                               },
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "operatingSystem",
                                   Value = "Windows 2016",
                                   FakeComplex = new() 
                                   {
                                       FirstName = "Complex test4",
                                       AnotherFakeType = new() { Number = Random.Shared.Next(0, 1000) }
                                   }
                               }
                           }
                       }
                    },
                    new()
                    {                       
                       DateAdded = DateTime.Now,
                       FakeObjectOne = new()
                        {
                            Value = Random.Shared.Next(0, 1000),
                            InternalFakeObject = new()
                            {
                                MyValue = $"My number is: {Random.Shared.Next(0, 1000)}"
                            }
                        },
                        FakeObjectTwo = new()
                        {
                            Value = Random.Shared.Next(0, 1000),
                        },
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
                                   Path = "/path/to/dc3/backup1.vhdx",
                                   Credentials = new()
                                   {
                                       Username = "rebecca@meta.com",
                                       Password = "mypassword3"
                                   },
                                   NetworkInformation = new()
                                   {
                                       Address = "www.facebook.com"
                                   }
                               }
                           },
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "dnsHostName",
                                   Value = "dc3.contoso.com",
                                   FakeComplex = new() 
                                   { 
                                       FirstName = "Complex test5",
                                       AnotherFakeType = new() { Number = Random.Shared.Next(0, 1000) }
                                   }
                               },
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "operatingSystem",
                                   Value = "Windows 2012R2",
                                   FakeComplex = new() 
                                   { 
                                       FirstName = "Complex test6",
                                       AnotherFakeType = new() { Number = Random.Shared.Next(0, 1000) }
                                   }
                               }
                           }
                       }
                    },
                    new()
                    {                        
                       DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(20)),
                       FakeObjectOne = new()
                        {
                            Value = Random.Shared.Next(0, 1000),
                            InternalFakeObject = new()
                            {
                                MyValue = $"My number is: {Random.Shared.Next(0, 1000)}"
                            }
                        },
                        FakeObjectTwo = new()
                        {
                            Value = Random.Shared.Next(0, 1000),
                        },
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
                                   Path = "/path/to/dc4/backup1.vhdx",
                                   Credentials = new()
                                   {
                                       Username = "piper@contoso.com",
                                       Password = "mypassword4"
                                   },
                                   NetworkInformation = new()
                                   {
                                       Address = "www.contoso.com"
                                   }
                               },
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest1,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(1)),
                                   Path = "/path/to/dc4/backup2.vhdx",
                                   Credentials = new()
                                   {
                                       Username = "adam@shopify.ca",
                                       Password = "mypassword5"
                                   },
                                   NetworkInformation = new()
                                   {
                                       Address = "www.shopify.com"
                                   }
                               }
                           },
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "dnsHostName",
                                   Value = "dc4.contoso.com",
                                   FakeComplex = new() 
                                   {
                                       FirstName = "Complex test7",
                                       AnotherFakeType = new() { Number = Random.Shared.Next(0, 1000) }
                                   }
                               },
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "sAMAccountName",
                                   Value = "DC4",
                                   FakeComplex = new() 
                                   { 
                                       FirstName = "Complex test8",
                                       AnotherFakeType = new() { Number = Random.Shared.Next(0, 1000) }
                                   }
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
                FakeType = new() 
                { 
                    FirstName = "Piper",
                    AnotherFakeType = new() { Number = 42 }
                },
                AdObjects = new List<AdObject>
                {
                    new()
                    {                        
                       DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(50)),
                       FakeObjectOne = new()
                        {
                            Value = Random.Shared.Next(0, 1000),
                            InternalFakeObject = new()
                            {
                                MyValue = $"My number is: {Random.Shared.Next(0, 1000)}"
                            }
                        },
                        FakeObjectTwo = new()
                        {
                            Value = Random.Shared.Next(0, 1000),
                        },
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
                                   Path = "/path/to/dc1/backup1.vhdx",
                                   Credentials = new()
                                   {
                                       Username = "eric@contoso.ca",
                                       Password = "mypassword6"
                                   },
                                   NetworkInformation = new()
                                   {
                                       Address = "www.contoso.ca"
                                   }
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
                       DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(15)),
                       FakeObjectOne = new()
                        {
                            Value = Random.Shared.Next(0, 1000),
                            InternalFakeObject = new()
                            {
                                MyValue = $"My number is: {Random.Shared.Next(0, 1000)}"
                            }
                        },
                        FakeObjectTwo = new()
                        {
                            Value = Random.Shared.Next(0, 1000),
                        },
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
                                   Path = "/path/to/dc2/backup1.vhdx",
                                   Credentials = new()
                                   {
                                       Username = "amanda@contoso.ca",
                                       Password = "mypassword7"
                                   },
                                   NetworkInformation = new()
                                   {
                                       Address = "www.contoso.ca"
                                   }
                               },
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest2,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(5)),
                                   Path = "/path/to/dc2/backup2.vhdx",
                                   Credentials = new()
                                   {
                                       Username = "jessica@contoso.ca",
                                       Password = "mypassword7"
                                   },
                                   NetworkInformation = new()
                                   {
                                       Address = "www.contoso.ca"
                                   }
                               },
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest2,
                                   DateCreated = DateTime.Now,
                                   Path = "/path/to/dc2/backup3.vhdx",
                                   Credentials = new()
                                   {
                                       Username = "angela@contoso.ca",
                                       Password = "mypassword8"
                                   },
                                   NetworkInformation = new()
                                   {
                                       Address = "www.contoso.ca"
                                   }
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
                       DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(1)),
                       FakeObjectOne = new()
                        {
                            Value = Random.Shared.Next(0, 1000),
                            InternalFakeObject = new()
                            {
                                MyValue = $"My number is: {Random.Shared.Next(0, 1000)}"
                            }
                        },
                        FakeObjectTwo = new()
                        {
                            Value = Random.Shared.Next(0, 1000),
                        },
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
                                   FakeComplex = new() 
                                   { 
                                       FirstName = "Complex test9",
                                       AnotherFakeType = new() { Number = Random.Shared.Next(0, 1000) }
                                   }
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
                       DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(20)),
                       FakeObjectOne = new()
                        {
                            Value = Random.Shared.Next(0, 1000),
                            InternalFakeObject = new()
                            {
                                MyValue = $"My number is: {Random.Shared.Next(0, 1000)}"
                            }
                        },
                        FakeObjectTwo = new()
                        {
                            Value = Random.Shared.Next(0, 1000),
                        },
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
                                   Path = "/path/to/dc4/backup1.vhdx",
                                   Credentials = new()
                                   {
                                       Username = "peter@contoso.ca",
                                       Password = "mypassword8"
                                   },
                                   NetworkInformation = new()
                                   {
                                       Address = "www.contoso.ca"
                                   }
                               },
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest2,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(1)),
                                   Path = "/path/to/dc4/backup2.vhdx",
                                   Credentials = new()
                                   {
                                       Username = "richard@contoso.ca",
                                       Password = "mypassword9"
                                   },
                                   NetworkInformation = new()
                                   {
                                       Address = "www.contoso.ca"
                                   }
                               }
                           },
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "dnsHostName",
                                   Value = "dc4.google.com",
                                   FakeComplex = new() 
                                   { 
                                       FirstName = "Complex test11",
                                       AnotherFakeType = new() { Number = Random.Shared.Next(0, 1000) }
                                   }
                               },
                               new()
                               {
                                   //Id = Guid.NewGuid(),
                                   Name = "operatingSystem",
                                   Value = "Windows 2008",
                                   FakeComplex = new() 
                                   { 
                                       FirstName = "Complex test12",
                                       AnotherFakeType = new() { Number = Random.Shared.Next(0, 1000) }
                                   }
                               }
                           }
                       }
                    },
                }
            },
        };
}
}
