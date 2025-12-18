using Microsoft.EntityFrameworkCore.Storage;
using Server.DataBase.Data;
using Server.DataBase.Entities;
using System;
using System.Threading.Tasks;

namespace Server.DataBase.Repositories
{
    public class UnitOfWork
    {
        private readonly GameDbContext context;
        private IDbContextTransaction transaction;

        private Repository<Player> players;
        private Repository<Character> characters;
        private Repository<Entity> entities;
        private Repository<Friend> friends;

        private Repository<FriendGroup> friendGroups;
        private Repository<FriendRequest> friendRequests;
        private Repository<PrivateMessage> privateMessages;

        private Repository<Mail> mails;
        private Repository<MailAttachment> mailAttachments;

        public UnitOfWork(GameDbContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Repository<Player> Players => players ??= new Repository<Player>(context);
        public Repository<Character> Characters => characters ??= new Repository<Character>(context);
        public Repository<Entity> Entities => entities ??= new Repository<Entity>(context);
        public Repository<Friend> Friends => friends ??= new Repository<Friend>(context);

        public Repository<FriendGroup> FriendGroups => friendGroups ??= new Repository<FriendGroup>(context);
        public Repository<FriendRequest> FriendRequests => friendRequests ??= new Repository<FriendRequest>(context);
        public Repository<PrivateMessage> PrivateMessages => privateMessages ??= new Repository<PrivateMessage>(context);

        public Repository<Mail> Mails => mails ??= new Repository<Mail>(context);
        public Repository<MailAttachment> MailAttachments => mailAttachments ??= new Repository<MailAttachment>(context);

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
