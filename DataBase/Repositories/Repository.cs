using Dapper;
using Microsoft.EntityFrameworkCore;
using Server.DataBase.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Server.DataBase.Repositories
{
    /// <summary>
    /// 通用仓储实现
    /// 结合EF Core和Dapper,提供灵活的数据访问
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public class Repository<T> where T : class
    {
        protected readonly GameDbContext context;
        protected readonly DbSet<T> dbSet;

        public Repository(GameDbContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            Console.WriteLine(typeof(T).Name);
            dbSet = context.Set<T>();
        }

        #region 查询方法 (EF Core)

        public virtual async Task<T> GetByIdAsync(object id)
        {
            return await dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await dbSet.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await dbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await dbSet.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await dbSet.AnyAsync(predicate);
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null)
        {
            if (predicate == null)
                return await dbSet.CountAsync();

            return await dbSet.CountAsync(predicate);
        }


        #endregion

        #region 添加方法

        public virtual async Task<T> AddAsync(T entity)
        {
            await dbSet.AddAsync(entity);
            return entity;
        }

        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await dbSet.AddRangeAsync(entities);
        }

        #endregion

        #region 更新方法

        public virtual void Update(T entity)
        {
            dbSet.Update(entity);
        }

        public virtual void UpdateRange(IEnumerable<T> entities)
        {
            dbSet.UpdateRange(entities);
        }

        #endregion

        #region 删除方法

        public virtual void Delete(T entity)
        {
            dbSet.Remove(entity);
        }

        public virtual void DeleteRange(IEnumerable<T> entities)
        {
            dbSet.RemoveRange(entities);
        }

        public virtual async Task DeleteByIdAsync(object id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                Delete(entity);
            }
        }

        #endregion

        #region 原生SQL方法 (Dapper)

        /// <summary>
        /// 获取数据库连接 (用于Dapper)
        /// </summary>
        protected IDbConnection GetConnection()
        {
            return context.Database.GetDbConnection();
        }

        public virtual async Task<IEnumerable<T>> QueryAsync(string sql, object param = null)
        {
            var connection = GetConnection();
            return await connection.QueryAsync<T>(sql, param);
        }

        public virtual async Task<T> QuerySingleOrDefaultAsync(string sql, object param = null)
        {
            var connection = GetConnection();
            return await connection.QuerySingleOrDefaultAsync<T>(sql, param);
        }

        public virtual async Task<T> QueryFirstOrDefaultAsync(string sql, object param = null)
        {
            var connection = GetConnection();
            return await connection.QueryFirstOrDefaultAsync<T>(sql, param);
        }

        public virtual async Task<int> ExecuteAsync(string sql, object param = null)
        {
            var connection = GetConnection();
            return await connection.ExecuteAsync(sql, param);
        }

        public virtual async Task<TResult> ExecuteScalarAsync<TResult>(string sql, object param = null)
        {
            var connection = GetConnection();
            return await connection.ExecuteScalarAsync<TResult>(sql, param);
        }

        #endregion
    }
}

