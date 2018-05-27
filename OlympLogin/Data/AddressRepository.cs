using System;
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

            return streets;
        }

        public async Task<Region> GetTerritoryByCode(string code)
        {
            var result = await _context.Territory.AsNoTracking()
                .Join(_context.Abbreviation, t => t.Abbreviation, a => a.ShortName, (terr, abbr) => new Region
                {
                    Name = terr.Name,
                    Type = abbr.FullName,
                    Code = terr.Code,
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
            var regions = _context.Regions.AsNoTracking()
                .OrderBy(region => region.Name)
                .Select(region =>
                    new SelectListItem
                    {
                        Value = region.Code,
                        Text = $"{region.Type} {region.Name}"
                    }).ToList();
            tip.AddRange(regions);
            return tip;
        }

        public async Task<(string, string)> MakeAddress(UserRegisterViewModel model)
        {
            var region = await GetRegionByCode(model.SelectedRegion);
            var city = await GetTerritoryByCode(model.SelectedCity);
            var street = await GetStreetByCode(model.SelectedStreet);
            var result =
                $"{region.Type} {region.Name}, {city.Type} {GetFullName(city.Code, city.Name)}, {street.Name}, д. {model.Building}, кв. {model.Flat}";
            return (result, street.Index);
        }
    }
}