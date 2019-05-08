using MD.Model.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.DB.Repositorys
{
    public static class RepositoryHelper
    {
        public static T UpdateContextItem<T>(DbContext context,T obj) where T :class 
        {
            if (obj == null)
                return null;
            List<PropertyInfo> _list = new List<PropertyInfo>();
            //将foreign key制空
            obj.GetType().GetProperties().ToList().ForEach(delegate (PropertyInfo pi)
            {
                var att = pi.GetCustomAttribute(typeof(ForeignKeyAttribute));
                if (att != null)
                {
                    var v = pi.GetValue(obj);
                    if(v==null)
                        _list.Add(pi);
                }
            });

            if(_list.Count==0)
                return obj;
            
            //修改操作为修改
            context.Entry(obj).State = EntityState.Modified;

            //重新加载foreign key的对象。
            foreach (var pi in _list)
            {
                context.Entry(obj).Reference(pi.Name).Load();
            }

            return obj;
        }

        public static List<T> UpdateContextItems<T>(DbContext context, List<T> list) where T : class
        {
            if (list == null)
                return null;
            foreach (T v in list)
            {
                UpdateContextItem(context, v);
            }
            return list;
        }
    }
}
