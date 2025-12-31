using Microsoft.EntityFrameworkCore.Storage;
using Server.DataBase.Entities;
using System;
using System.Threading.Tasks;

namespace Server.DataBase
{
    public class UnitOfWork
    {
        private readonly GameDbContext context;
        private IDbContextTransaction transaction;

        private Repository<Player> players;
        private Repository<Character> characters;
        private Repository<InventoryItem> inventoryItems;
        private Repository<WeaponMastery> weaponMasteries;

        public UnitOfWork(GameDbContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Repository<Player> Players => players ??= new Repository<Player>(context);
        public Repository<Character> Characters => characters ??= new Repository<Character>(context);
        public Repository<InventoryItem> InventoryItems => inventoryItems ??= new Repository<InventoryItem>(context);
        public Repository<WeaponMastery> WeaponMasteries => weaponMasteries ??= new Repository<WeaponMastery>(context);

        public async Task<int> SaveChangesAsync() => await context.SaveChangesAsync();

        public async Task BeginTransactionAsync()
        {
            if (transaction != null) throw new InvalidOperationException("事务已经开始");
            transaction = await context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            if (transaction == null) throw new InvalidOperationException("没有活动的事务");
            try
            {
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await RollbackAsync();
                throw;
            }
            finally
            {
                await transaction.DisposeAsync();
                transaction = null;
            }
        }

        public async Task RollbackAsync()
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
                await transaction.DisposeAsync();
                transaction = null;
            }
        }

        public void Dispose()
        {
            transaction?.Dispose();
            transaction = null;
            context?.Dispose();
        }
    }
}
