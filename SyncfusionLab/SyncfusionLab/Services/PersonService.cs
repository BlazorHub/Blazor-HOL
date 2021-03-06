﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyncfusionLab.Services
{
    using EFCoreModel.Models;
    using Microsoft.EntityFrameworkCore;
    using SyncfusionLab.Extensions;

    public class PersonService : IPersonService
    {
        private readonly SchoolContext context;

        public PersonService(SchoolContext context)
        {
            this.context = context;
        }

        public Task<IQueryable<Person>> GetAsync()
        {
            return Task.FromResult(context.Person
                .AsNoTracking()
                .AsQueryable());
        }

        public async Task<Person> GetAsync(int id)
        {
            Person item = await context.Person
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PersonId == id);
            return item;
        }

        public async Task<bool> AddAsync(Person paraObject)
        {
            await context.Person
                .AddAsync(paraObject);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(Person paraObject)
        {
            #region EF Core 追蹤查詢所造成的問題說明
            // 若再進行搜尋該修改紀錄的時候，使用了 追蹤查詢 (也就是，沒有使用 .AsNoTracking()方法)
            // 將會造成快取記錄在電腦端，而這裡若要進行 
            // context.Entry(paraObject).State = EntityState.Modified; 呼叫與更新的時候
            // 將會造成問題
            // System.InvalidOperationException: The instance of entity type 'Person' cannot be tracked 
            // because another instance with the same key value for {'PersonId'} is already being tracked. 
            // When attaching existing entities, ensure that only one entity instance with a given key value
            // is attached. Consider using 'DbContextOptionsBuilder.EnableSensitiveDataLogging' 
            // to see the conflicting key values.
            #endregion

            Person item = await context.Person
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PersonId == paraObject.PersonId);
            if (item == null)
            {
                return false;
            }
            else
            {
                #region 在這裡需要設定需要解除快取紀錄
                context.CleanAllEFCoreTracking<Person>();
                #endregion
                // set Modified flag in your entry
                context.Entry(paraObject).State = EntityState.Modified;

                // save 
                await context.SaveChangesAsync();
                return true;
            }

        }

        public async Task<bool> DeleteAsync(Person paraObject)
        {
            Person item = await context.Person
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PersonId == paraObject.PersonId);
            if (item == null)
            {
                return false;
            }
            else
            {
                context.CleanAllEFCoreTracking<Person>();
                context.Person.Remove(item);
                await context.SaveChangesAsync();
                return true;
            }
        }
    }
}
