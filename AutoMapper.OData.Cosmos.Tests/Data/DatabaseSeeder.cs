using AutoMapper.OData.Cosmos.Tests.Entities;

namespace AutoMapper.OData.Cosmos.Tests.Data;

internal static class DatabaseSeeder
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
                Name = "Abernathy Forest",
                ForestWideCredentials = new()
                {
                    Username = "AbernathyAdministrator",
                    Password = "^1ScfqS8s939I%xU"
                },
                DomainControllers = new List<DomainControllerEntry>
                {
                    new()
                    {
                        DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(12)),
                        DcCredentials = new()
                        {
                            Username = "administrator",
                            Password = "ch8d7F7YI6v6!BRx"
                        },
                        DcNetworkInformation = new()
                        {
                            Address = "http://www.abernathy.com/"
                        },
                        Dc = new()
                        {
                           Id = Guid.NewGuid(),
                           ForestId = forest1,
                           Fqdn = "dc1.abernathy.com",
                           FsmoRoles = new List<FsmoRole>
                           {
                               FsmoRole.PdcEmulator,
                               FsmoRole.DomainNamingMaster
                           },
                           Backups = new List<Backup>
                           {
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest1,
                                   DateCreated = DateTime.Now,
                                   Location = new()
                                   {
                                       Credentials = new()
                                       {
                                            Username = "admin@abernathy.com",
                                            Password = "q70Z%T2i$T8Tomm*"
                                       },
                                       NetworkInformation = new()
                                       {
                                            Address = "Azure blob storage"
                                       }
                                   }
                               },
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest1,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(10)),
                                   Location = new()
                                   {
                                       Credentials = new()
                                       {
                                            Username = "admin@abernathy.com",
                                            Password = "#w#28N0iiT&#!u*T"
                                       },
                                       NetworkInformation = new()
                                       {
                                            Address = "/path/to/secure/storage"
                                       }
                                   }
                               }
                           },
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {
                                   Name = "dnsHostName",
                                   Value = "dc1.abernathy.com"
                               },
                               new()
                               {                                   
                                   Name = "operatingSystem",
                                   Value = "Windows 2019"
                               }
                           }
                       }
                    },
                    new()
                    {
                        DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(15)),
                        DcCredentials = new()
                        {
                            Username = "administrator",
                            Password = "cS6Xxs7z3Q4xS^KU"
                        },
                        DcNetworkInformation = new()
                        {
                            Address = "http://www.abernathy.com/"
                        },
                        Dc = new()
                        {
                           Id = Guid.NewGuid(),
                           ForestId = forest1,
                           Fqdn = "dc2.abernathy.com",
                           FsmoRoles = new List<FsmoRole> 
                           { 
                               FsmoRole.RidMaster, 
                               FsmoRole.InfrastructureMaster 
                           },
                           Backups = new List<Backup>
                           {
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest1,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(5)),
                                   Location = new()
                                   {
                                       Credentials = new()
                                       {
                                            Username = "admin@abernathy.com",
                                            Password = "47NIb!nOx!Smz5Bk"
                                       },
                                       NetworkInformation = new()
                                       {
                                            Address = "Azure blob storage"
                                       }
                                   }
                               },
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest1,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(1)),
                                   Location = new()
                                   {
                                       Credentials = new()
                                       {
                                            Username = "admin@abernathy.com",
                                            Password = "um4WnuW$5k5gpD3G"
                                       },
                                       NetworkInformation = new()
                                       {
                                            Address = "/path/to/secure/storage"
                                       }
                                   }
                               }
                           },
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {
                                   Name = "dc2.abernathy.com",
                                   Value = "dc2.contoso.com"
                               },
                               new()
                               {
                                   Name = "operatingSystem",
                                   Value = "Windows 2016"
                               }
                           }
                       }
                    },
                    new()
                    {
                       DateAdded = DateTime.Now,
                       DcCredentials = new()
                       {
                           Username = "administrator",
                           Password = ""
                       },
                       DcNetworkInformation = new()
                       {
                           Address = "http://www.abernathy.com/"
                       },
                       Dc = new()
                       {
                           Id = Guid.NewGuid(),
                           ForestId = forest1,
                           Fqdn = "dc3.abernathy.com",
                           Backups = new List<Backup>
                           {
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest1,
                                   DateCreated = DateTime.Now,
                                   Location = new()
                                   {
                                       Credentials = new()
                                       {
                                            Username = "admin@abernathy.com",
                                            Password = "bfMUNc3g^T8N&@Wq"
                                       },
                                       NetworkInformation = new()
                                       {
                                            Address = "Azure blob storage"
                                       }
                                   }
                               }
                           },
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {                                
                                   Name = "dnsHostName",
                                   Value = "dc3.abernathy.com"
                               },
                               new()
                               {                                   
                                   Name = "operatingSystem",
                                   Value = "Windows 2012R2"
                               }
                           }
                       }
                    },
                    new()
                    {
                       DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(20)),
                       DcCredentials = new()
                       {
                           Username = "administrator",
                           Password = "r7j&eaN5OLWx*27S"
                       },
                       DcNetworkInformation = new()
                       {
                           Address = "http://www.abernathy.com/"
                       },
                       Dc = new()
                       {
                           Id = Guid.NewGuid(),
                           ForestId = forest1,
                           Fqdn = "dc4.abernathy.com",
                           FsmoRoles = new List<FsmoRole> 
                           { 
                               FsmoRole.SchemaMaster 
                           },
                           Backups = new List<Backup>
                           {
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest1,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(5)),
                                   Location = new()
                                   {
                                       Credentials = new()
                                       {
                                            Username = "admin@abernathy.com",
                                            Password = "bfMUNc3g^T8N&@Wq"
                                       },
                                       NetworkInformation = new()
                                       {
                                            Address = "/path/to/secure/storage"
                                       }
                                   }
                               },
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest1,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(1)),
                                   Location = new()
                                   {
                                       Credentials = new()
                                       {
                                            Username = "admin@abernathy.com",
                                            Password = "O4jeuZx03tw8&nDm"
                                       },
                                       NetworkInformation = new()
                                       {
                                            Address = "/path/to/secure/storage"
                                       }
                                   }
                               }
                           },
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {                                   
                                   Name = "dnsHostName",
                                   Value = "dc4.abernathy.com"
                               },
                               new()
                               {                                   
                                   Name = "sAMAccountName",
                                   Value = "DC4"
                               }
                           }
                       }
                    },
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Rolfson Forest",
                ForestId = forest2,
                ForestWideCredentials = new()
                {
                    Username = "RolfsonAdministrator",
                    Password = "zH1s6Y@1O069$^E0"
                },
                DomainControllers = new List<DomainControllerEntry>
                {
                    new()
                    {
                       DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(50)),
                       DcCredentials = new()
                       {
                           Username = "administrator",
                           Password = "ch8d7F7YI6v6!BRx"
                       },
                       DcNetworkInformation = new()
                       {
                           Address = "http://www.rolfson.com/"
                       },
                       Dc = new()
                       {
                           Id = Guid.NewGuid(),
                           ForestId = forest2,
                           Fqdn = "dc1.rolfson.com",
                           Backups = new List<Backup>
                           {
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest2,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(25)),
                                   Location = new()
                                   {
                                       Credentials = new()
                                       {
                                            Username = "admin@rolfson.com",
                                            Password = "q70Z%T2i$T8Tomm*"
                                       },
                                       NetworkInformation = new()
                                       {
                                            Address = "Azure blob storage"
                                       }
                                   }
                               }
                           },
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {
                                   Name = "dnsHostName",
                                   Value = "dc1.rolfson.com"
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
                       DcCredentials = new()
                       {
                           Username = "administrator",
                           Password = "YHLNQc^ZKPu%6H4Z"
                       },
                       DcNetworkInformation = new()
                       {
                           Address = "http://www.rolfson.com/"
                       },
                       Dc = new()
                       {
                           Id = Guid.NewGuid(),
                           ForestId = forest2,
                           Fqdn = "dc2.rolfson.com",
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
                                   Location = new()
                                   {
                                       Credentials = new()
                                       {
                                            Username = "admin@rolfson.com",
                                            Password = "47x*$L4VRDz3sx*9"
                                       },
                                       NetworkInformation = new()
                                       {
                                            Address = "path/to/secure/storage"
                                       }
                                   }
                               },
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest2,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(5)),
                                   Location = new()
                                   {
                                       Credentials = new()
                                       {
                                            Username = "admin@rolfson.com",
                                            Password = "8seGmape^4ZEF#2m"
                                       },
                                       NetworkInformation = new()
                                       {
                                            Address = "path/to/secure/storage"
                                       }
                                   }
                               },
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest2,
                                   DateCreated = DateTime.Now,
                                   Location = new()
                                   {
                                       Credentials = new()
                                       {
                                            Username = "admin@rolfson.com",
                                            Password = "6o%&Xk1&6Zz3&#eP"
                                       },
                                       NetworkInformation = new()
                                       {
                                            Address = "Azure blob storage"
                                       }
                                   }
                               }
                           },
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {
                                   Name = "dnsHostName",
                                   Value = "dc2.rolfson.com"
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
                       DcCredentials = new()
                       {
                           Username = "administrator",
                           Password = "U0^8WP^a2PWeh#sV"
                       },
                       DcNetworkInformation = new()
                       {
                           Address = "http://www.rolfson.com/"
                       },
                       Dc = new()
                       {
                           Id = Guid.NewGuid(),
                           ForestId = forest2,
                           Fqdn = "dc3.rolfson.com",
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {
                                   Name = "dnsHostName",
                                   Value = "dc3.rolfson.com"
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
                       DateAdded = DateTime.Now.Subtract(TimeSpan.FromDays(20)),
                       DcCredentials = new()
                       {
                           Username = "administrator",
                           Password = "x5@v#7T4lC@3Xj4EU0^8WP^a2PWeh#sV"
                       },
                       DcNetworkInformation = new()
                       {
                           Address = "http://www.rolfson.com/"
                       },
                       Dc = new()
                       {
                           Id = Guid.NewGuid(),
                           ForestId = forest2,
                           Fqdn = "dc4.rolfson.com",
                           FsmoRoles = new List<FsmoRole> 
                           { 
                               FsmoRole.RidMaster 
                           },
                           Backups = new List<Backup>
                           {
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest2,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(5)),
                                   Location = new()
                                   {
                                       Credentials = new()
                                       {
                                            Username = "admin@rolfson.com",
                                            Password = "ndFhEj@K8&5z&uBM"
                                       },
                                       NetworkInformation = new()
                                       {
                                            Address = "path/to/secure/storage"
                                       }
                                   }
                               },
                               new Backup
                               {
                                   Id = Guid.NewGuid(),
                                   ForestId = forest2,
                                   DateCreated = DateTime.Now.Subtract(TimeSpan.FromDays(1)),
                                   Location = new()
                                   {
                                       Credentials = new()
                                       {
                                            Username = "admin@rolfson.com",
                                            Password = "ndFhEj@K8&5z&uBM"
                                       },
                                       NetworkInformation = new()
                                       {
                                            Address = "Azure blob storage"
                                       }
                                   }
                               }
                           },
                           Attributes = new List<ObjectAttribute>
                           {
                               new()
                               {                                   
                                   Name = "dnsHostName",
                                   Value = "dc4.rolfson.com"
                               },
                               new()
                               {
                                   Name = "operatingSystem",
                                   Value = "Windows 2008"
                               }
                           }
                       }
                    },
                }
            },
        };
    }
}
