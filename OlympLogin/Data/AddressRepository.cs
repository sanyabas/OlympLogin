using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OlympLogin.Models;
using OlympLogin.ViewModels;

namespace OlympLogin.Data
{
    public class AddressRepository
    {
        private readonly kladrContext _context;
        private readonly string regionCode;
        private Dictionary<string, string> districts;
        private Dictionary<string, string> cities;

        public AddressRepository(kladrContext context, string regionCode)
        {
            _context = context;
            this.regionCode = regionCode;
            Preload();
        }

        public AddressRepository(kladrContext context)
        {
            _context = context;
        }

        public void Preload()
        {
            Task.WaitAll(LoadCities(), LoadDistricts());
        }

        private async Task LoadDistricts()
        {
            if (string.IsNullOrEmpty(regionCode))
                throw new ArgumentException();
            var distrPattern = $"{regionCode}___00000000";
            var rayons = await _context.Territory.AsNoTracking()
                .Join(_context.Abbreviation, t => t.Abbreviation, a => a.ShortName, (terr, abbr) => new
                {
                    Name = terr.Name,
                    Type = abbr.FullName,
                    Code = terr.Code,
                    Level = abbr.Level
                })
                .Where(ter => EF.Functions.Like(ter.Code, distrPattern) && ter.Level == "2")
                .ToDictionaryAsync(ter => ter.Code.Substring(2, 3), ter => $"{ter.Type} {ter.Name}");
            districts = rayons;
        }

        private async Task LoadCities()
        {
            if (string.IsNullOrEmpty(regionCode))
                throw new ArgumentException();
            var cityPattern = $"{regionCode}______00000";
            var citiesResult = await _context.Territory.AsNoTracking()
                .Join(_context.Abbreviation, t => t.Abbreviation, a => a.ShortName, (terr, abbr) => new
                {
                    Name = terr.Name,
                    Type = abbr.FullName,
                    Code = terr.Code,
                    Level = abbr.Level,
                    Index = terr.Index
                })
                .Where(ter => EF.Functions.Like(ter.Code, cityPattern) && ter.Level == "3")
                //.OrderBy(ter => ter.Name)
                .ToDictionaryAsync(ter => ter.Code, ter => $"{ter.Type} {ter.Name}");
            cities = citiesResult;
            //.Select(ter => new SelectListItem
            //{
            //    Value = ter.Code,
            //    Text = $"{ter.Type} {ter.Name}"
            //}).ToList();
            //return cities;
        }

        private string GetFullName(string code, string name)
        {
            if (code.Substring(5, 3) == "000")
            {
                var extracted = districts.TryGetValue(code.Substring(2, 3), out var district);
                return extracted ? $"{name} ({district})" : name;
            }

            return code.Substring(8, 3) == "000" ? name : $"{name} ({cities[$"{code.Substring(0, 8)}00000"]})";

            //return code.Substring(8, 3) == "000"
            //    ? $"{name} ({districts[code.Substring(2, 3)]} р-н)"
            //    : $"{name} (г. {cities[$"{code.Substring(0, 8)}00000"]})";
        }

        public IEnumerable<SelectListItem> GetLocalities()
        {
            var result = cities.Select(city => new SelectListItem
            {
                Value = city.Key,
                Text = city.Value
            }).ToList();
            //var villageReg = new Regex($"{regionCode}\\d{{9}}00");
            var villagePattern = $"{regionCode}_________00";
            var villages = _context.Territory.AsNoTracking()
                .Join(_context.Abbreviation, t => t.Abbreviation, a => a.ShortName, (terr, abbr) => new
                {
                    Name = terr.Name,
                    Type = abbr.FullName,
                    Code = terr.Code,
                    Level = abbr.Level,
                    Index = terr.Index
                })
                .Where(vil =>
                    vil.Level == "4" && EF.Functions.Like(vil.Code, villagePattern) &&
                    vil.Code.Substring(8, 3) != "000")
                .ToList();
            Console.WriteLine(villages);
            var r = villages
                .Select(vil => new SelectListItem
                //{
                //    var newName = vil.Code.Substring(8, 3) == "000"
                //        ? $"{vil.Name} ({districts[vil.Code.Substring(2, 3)]} р-н)"
                //        : $"{vil.Name} (г. {cities[$"{vil.Code.Substring(0, 8)}00000"]})";
                //    return new
                {
                    Text = $"{vil.Type} {GetFullName(vil.Code, vil.Name)}",
                    Value = vil.Code
                    //};
                });
            result.AddRange(r);
            return result;
            //foreach (var village in villages)
            //{
            //    if (village.Code.Substring(8, 3) == "000")
            //    {
            //        village.Name = $"{village.Name} ({districts[village.Code.Substring(2, 3)]})";
            //    }
            //}
            //    .ForEach(vil =>
            //    {
            //        if (vil.Code.Substring(8, 3) == "000")
            //        {
            //            vil
            //        } //if there's no city
            //    })
        }

        public IEnumerable<SelectListItem> GetStreets(string locCode)
        {
            if (string.IsNullOrEmpty(locCode))
                return null;
            var locPattern = $"{locCode.Substring(0, 11)}____00";
            var streets = _context.Street.AsNoTracking()
                .Join(_context.Abbreviation, s => s.Abbr, a => a.ShortName, (street, abbr) => new
                {
                    Name = street.Name,
                    Type = abbr.FullName,
                    Code = street.Code,
                    Index = street.Index,
                    Level = abbr.Level
                })
                .Where(ter => ter.Level == "5" && EF.Functions.Like(ter.Code, locPattern))
                .OrderBy(ter => ter.Name)
                .Select(ter => new SelectListItem
                {
                    Value = ter.Code,
                    Text = $"{ter.Type} {ter.Name}"
                });

            if (!streets.Any())
            {
                return new[] {new SelectListItem
                {
                    Value=null,
                    Text="Нет улиц"
                }};
            }

            return streets;
        }

        public IEnumerable<SelectListItem> GetBuildings(string streetCode)
        {
            var numReg = new Regex("\\d+.*");
            if (string.IsNullOrEmpty(streetCode))
                return null;
            var strPattern = $"{streetCode.Substring(0, 15)}____";
            var buildings = _context.Building
                //.AsNoTracking()
                .Where(house => EF.Functions.Like(house.Code, strPattern))
                .ToList()
                .SelectMany(house => house.Name.Split(",", StringSplitOptions.None)
                    .Select(x => new Building
                    {
                        Code = $"${house.Code}_${numReg.Match(x).Value}",
                        Index = house.Index,
                        Name = numReg.Match(x).Value
                    }))
                .OrderBy(house => house.Name)
                .Select(house => new SelectListItem
                {
                    Value = house.Code,
                    Text = house.Name
                });
            if (!buildings.Any())
            {
                return new[]
                {
                    new SelectListItem
                    {
                        Value = null,
                        Text = "Нет домов"
                    }
                };
            }

            return buildings;
        }

        public async Task<Territory> GetTerritoryByCode(string code)
        {
            var result = await _context.Territory.AsNoTracking()
                .Join(_context.Abbreviation, t => t.Abbreviation, a => a.ShortName, (terr, abbr) => new Territory
                {
                    Name = $"{abbr.FullName} {terr.Name}",
                    Code = terr.Code,
                    Index = terr.Index
                })
                .FirstOrDefaultAsync(city => city.Code == code);
            return result;
        }

        public async Task<Street> GetStreetByCode(string code)
        {
            var result = await _context.Street.AsNoTracking()
                .Join(_context.Abbreviation, s => s.Abbr, a => a.ShortName, (street, abbr) => new Street
                {
                    Name = $"{abbr.FullName} {street.Name}",
                    Code = street.Code,
                    Index = street.Index
                })
                .FirstOrDefaultAsync(str => str.Code == code);

            return result;
        }

        public async Task<Region> GetRegionByCode(string code)
        {
            return await _context.Regions.AsNoTracking().FirstOrDefaultAsync(reg => reg.Code == code);
        }

        private async Task<Building> GetBuildingByCode(string buildingCode)
        {
            return await _context.Building.AsNoTracking().FirstOrDefaultAsync(house => house.Code == buildingCode);
        }

        public IEnumerable<SelectListItem> GetRegions()
        {
            var tip = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = null,
                    Text = "Выберите регион"
                }
            };
            //};
            //var sverdl = new SelectListItem
            //{
            //    Value = "66",
            //    Text = "Область Свердловская"
            //};
            //var result = new List<SelectListItem> { sverdl };
            var regions = _context.Regions.AsNoTracking()
                .OrderBy(region => region.Name)
            //result.AddRange(regions);
            //var final = result
                .Select(region =>
                    new SelectListItem
                    {
                        Value = region.Code,
                        Text = $"{region.Type} {region.Name}",
                        Selected = region.Code == "66"
                    }).ToList();
            tip.AddRange(regions);
            return tip;
        }

        public async Task<(string, string)> MakeAddress(UserRegisterViewModel model)
        {
            var region = await GetRegionByCode(model.SelectedRegion);
            var city = await GetTerritoryByCode(model.SelectedCity);
            var street = model.SelectedStreet == null ? null : await GetStreetByCode(model.SelectedStreet);
            var building = model.SelectedBuilding == null ? null : await GetBuildingByCode(model.SelectedBuilding.Substring(0, 19));
            var parts = new List<string>
            {
                $"{region.Type} {region.Name}",
                $"{GetFullName(city.Code, city.Name)}"
            };
            string result;
            string index;
            if (street == null)
            {
                parts.AddRange(new[] {$"д. {model.BuildingName}",
                    $"кв. {model.Flat}"});
                result = string.Join(", ", parts);
                index = city.Index;
            }
            else
            {
                parts.AddRange(new[]{$"{street.Name}",
                    $"д. {model.BuildingName}",
                    $"кв. {model.Flat}"
                });
                result = string.Join(", ", parts);
                index = street.Index;
            }

            if (building?.Index == null)
                index = street?.Index ?? city.Index;
            else
                index = building.Index;

            return (result, index);

            //        $"{street.Name}",
            //            $"д. {model.Building}",
            //            $"кв. {model.Flat}"
            //        };
            //        if (street == null)
            //        {
            //            parts.RemoveAt(2);
            //        }

            //var result = string.Join(", ", parts);
            //        //var result =
            //        //    $"{region.Type} {region.Name}, {city.Type} {GetFullName(city.Code, city.Name)}, {street.Name}, д. {model.Building}, кв. {model.Flat}";
            //        return (result, street!=null? street.Index:c);
        }
    }
}