using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nooch.Data;
using System.Data.Entity;

namespace Nooch.Repository
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        
        protected readonly NOOCHEntities Context;
        

        public Repository(NOOCHEntities context)
        {
            Context = context;
            
        }

        public TEntity GetById(int id)
        {
            return Context.Set<TEntity>().Find(id);
        }

        public IEnumerable<TEntity> GetAll()
        {
            return Context.Set<TEntity>().ToList();
        }



        //public virtual IEnumerable<TEntity> Get()
        //{
        //    IQueryable<TEntity> query = dbSet;
        //    return query.ToList();
        //}

        //public virtual TEntity GetByID(object id)
        //{
        //    return dbSet.Find(id);
        //}

        //public virtual void Insert(TEntity entity)
        //{
        //    dbSet.Add(entity);
        //}

        //public virtual void Delete(object id)
        //{
        //    TEntity entityToDelete = dbSet.Find(id);
        //    Delete(entityToDelete);
        //}

        //public virtual void Delete(TEntity entityToDelete)
        //{
        //    if (_context.Entry(entityToDelete).State == EntityState.Detached)
        //    {
        //        dbSet.Attach(entityToDelete);
        //    }
        //    dbSet.Remove(entityToDelete);
        //}

        //public virtual void Update(TEntity entityToUpdate)
        //{
        //    dbSet.Attach(entityToUpdate);
        //    _context.Entry(entityToUpdate).State = EntityState.Modified;
        //}
    }
}
