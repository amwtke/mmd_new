using MD.Lib.DB.Context;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.DB.Code;
using Nest;

namespace MD.Lib.DB.Repositorys
{
    public class CodeRepository : IDisposable
    {
        private MDCodeContext context;

        #region Constructor
        public CodeRepository()
        {
            context = new MDCodeContext();
        }
        public CodeRepository(MDCodeContext context)
        {
            this.context = context;
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    context.Dispose();
                }
                disposedValue = true;
            }
        }

        // ~CodeRepository() {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        void IDisposable.Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Operation

        public async Task<CodeMerchantStatus> GetDaishenheStatusAsync()
        {
            return
                await (from status in context.MerchantStatus where status.code == 0 select status).FirstOrDefaultAsync();
        }
        #region brand
        static ConcurrentDictionary<int, string> _productBrandDic = new ConcurrentDictionary<int, string>();
        public ConcurrentDictionary<int, string> ProductBrandDic
        {
            get
            {
                if (_productBrandDic.IsEmpty)
                    GetAllProductBrand();
                return _productBrandDic;
            }
        }
        public List<Codebrand> GetAllProductBrand()
        {
            var list = (from j in context.Codebrands select j).ToList();
            foreach (var brand in list)
            {
                _productBrandDic[brand.code] = brand.value;
            }
            return list;
        }
        public List<Codebrand> GetAllProductBrand2()
        {
            var list = (from j in context.Codebrands select j).ToList();
            return list;
        }

        public Tuple<int, List<Codebrand>> GetProductBrand(int pageIndex, int pageSize, string q)
        {
            int from = (pageIndex - 1) * pageSize;
            if (string.IsNullOrEmpty(q))
            {
                var list = (from j in context.Codebrands orderby j.code select j).Skip(from).Take(pageSize).ToList();
                var count = (from j in context.Codebrands select j).Count();
                return Tuple.Create(count, list);
            }
            else
            {
                var list = (from j in context.Codebrands where j.value.Contains(q) orderby j.code select j).Skip(from).Take(pageSize).ToList();
                var count = (from j in context.Codebrands where j.value.Contains(q) select j).Count();
                return Tuple.Create(count, list);
            }
        }

        public async Task<bool> UpdateProductBrandAsync(int code, string value)
        {
            Codebrand brand = await (from b in context.Codebrands
                                     where b.code.Equals(code)
                                     select b).FirstOrDefaultAsync();
            if (brand != null)
            {
                brand.value = value;
                if (await context.SaveChangesAsync() == 1)
                {
                    _productBrandDic[brand.code] = value;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool AddBrand(string value)
        {
            int maxCode = context.Codebrands.Max(brand => brand.code);
            Codebrand b = new Codebrand();
            b.value = value;
            b.code = maxCode + 1;
            context.Codebrands.Add(b);
            if (context.SaveChanges() == 1)
            {
                _productBrandDic[b.code] = value;
                return true;
            }
            return false;
        }

        public async Task<bool> DelProductBrandAsync(int code)
        {
            Codebrand brand = await context.Codebrands.FirstOrDefaultAsync(b => b.code == code);
            if (brand != null)
            {
                context.Codebrands.Remove(brand);
                string value = "";
                if (await context.SaveChangesAsync() == 1)
                {
                    bool res = _productBrandDic.TryRemove(brand.code, out value);
                    return res;
                }
                return false;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region category

        public async Task<IEnumerable<string>> GetAllCategoryNameAsync()
        {
            var categoryNames = await context.ProductCategorys.Select(category => category.value).ToArrayAsync();
            if (categoryNames == null || categoryNames.Count() == 0)
                return null;
            return categoryNames;
        }

        static ConcurrentDictionary<int, string> _productCategoryDic = new ConcurrentDictionary<int, string>();
        static ConcurrentDictionary<string, int> _productCategoryDicReverse = new ConcurrentDictionary<string, int>();
        public async Task<List<CodeProductCategory>> GetAllProductCategoriesAsync()
        {
            var list = await (from code in context.ProductCategorys orderby code.sortid select code).ToListAsync();
            if (list != null && list.Count > 0)
            {
                foreach (var c in list)
                {
                    _productCategoryDic[c.code] = c.value;
                    _productCategoryDicReverse[c.value] = c.code;
                }
                return list;
            }
            return null;
        }

        public List<CodeProductCategory> GetAllProductCategories()
        {
            var list = (from code in context.ProductCategorys select code).ToList();
            if (list.Count > 0)
            {
                foreach (var c in list)
                {
                    _productCategoryDic[c.code] = c.value;
                    _productCategoryDicReverse[c.value] = c.code;
                }
                return list;
            }
            return null;
        }
        public List<CodeProductCategory> GetAllProductCategory()
        {
            var list = (from code in context.ProductCategorys select code).ToList();
            return list;
        }

        public ConcurrentDictionary<int, string> ProductCategoryDic
        {
            get
            {
                if (_productCategoryDic.IsEmpty)
                    GetAllProductCategories();
                return _productCategoryDic;
            }
        }

        public ConcurrentDictionary<string, int> ProductCategoryDicReverse
        {
            get
            {
                if (_productCategoryDicReverse.IsEmpty)
                    GetAllProductCategories();
                return _productCategoryDicReverse;
            }
        }
        #endregion

        #region taocan

        static ConcurrentDictionary<string, CodeBizTaocan> _tcDic = new ConcurrentDictionary<string, CodeBizTaocan>();
        public async Task<CodeBizTaocan> GetTaoCanByType(string type)
        {
            CodeBizTaocan ret = null;
            if (!_tcDic.TryGetValue(type, out ret))
            {
                ret =
                    await
                        (from code in context.BizTaoCan where code.tc_type.Equals(type) select code).FirstOrDefaultAsync
                            ();
                if (!string.IsNullOrEmpty(ret?.tc_type))
                    _tcDic[type] = ret;
            }
            return ret;
        }

        static ConcurrentDictionary<int, CodeMerPayType> _mPayDic = new ConcurrentDictionary<int, CodeMerPayType>();
        public async Task<CodeMerPayType> GetMPayType(int code)
        {
            CodeMerPayType ret = null;
            if (!_mPayDic.TryGetValue(code, out ret))
            {
                ret =
                    await
                        (from c in context.MerPayTypes where c.code == code select c).FirstOrDefaultAsync
                            ();
                if (ret?.code != null)
                    _mPayDic[code] = ret;
            }
            return ret;
        }
        #endregion
        #region taocanItem
        static ConcurrentDictionary<string, List<CodeBizTaocanItem>> _tcitemDic = new ConcurrentDictionary<string, List<CodeBizTaocanItem>>();
        public async Task<List<CodeBizTaocanItem>> GetTaoCanItemByType(string tc_type)
        {
            List<CodeBizTaocanItem> ret = null;
            if (!_tcitemDic.TryGetValue(tc_type, out ret))
            {
                ret = await
                    (from item in context.BizTaocanItems
                     where item.tc_type.Equals(tc_type)
                     select item).ToListAsync();
                if (ret != null)
                    _tcitemDic[tc_type] = ret;
            }
            return ret;
        }
        #endregion
        #region bizType
        static ConcurrentDictionary<string, CodeBizType> _dicbizType = new ConcurrentDictionary<string, CodeBizType>();
        public async Task<CodeBizType> GetCodeBizType(string biz_type)
        {
            CodeBizType ret = null;
            if (!_dicbizType.TryGetValue(biz_type, out ret))
            {
                ret = await (from j in context.BizTypes
                             where j.biz_type.Equals(biz_type)
                             select j).FirstOrDefaultAsync();
                if (ret != null)
                    _dicbizType[biz_type] = ret;
            }
            return ret;
        }
        #endregion

        #region Code_NoticeBoardStatus

        #endregion

        #region code_notice_category
        static ConcurrentDictionary<int, string> _dicNoticeCate = new ConcurrentDictionary<int, string>();
        public async Task<string> GetNoticeCateName(int code)
        {
            string retstring = "";
            if (!_dicNoticeCate.TryGetValue(code, out retstring))
            {
                var ret = await (from j in context.CodeNoticeCategory
                                 where j.code.Equals(code)
                                 select j).FirstOrDefaultAsync();
                if (ret != null)
                {
                    retstring = ret.name;
                    _dicNoticeCate[code] = ret.name;
                }
            }
            return retstring;
        }
        public async Task<ConcurrentDictionary<int, string>> GetAllNoticeCateAsync()
        {
            var list = await (from j in context.CodeNoticeCategory orderby j.sortid select j).ToListAsync();
            foreach (var item in list)
            {
                _dicNoticeCate[item.code] = item.name;
            }
            return _dicNoticeCate;
        }
        public async Task<List<CodeNoticeCategory>> GetNoticeCateListAsync()
        {
            return await context.CodeNoticeCategory.OrderBy(p => p.sortid).ToListAsync();
        }
        #endregion

        #region codeskin
        static ConcurrentDictionary<int, string> _dicSkin = new ConcurrentDictionary<int, string>();
        public async Task<string> GetCodeSkin(int code)
        {
            string retstring = "";
            if (!_dicSkin.TryGetValue(code, out retstring))
            {
                var ret = await (from j in context.CodeSkins
                                 where j.code.Equals(code)
                                 select j).FirstOrDefaultAsync();
                if (ret != null)
                {
                    retstring = ret.skin;
                    _dicSkin[code] = ret.skin;
                }
            }
            return retstring;
        }
        public async Task<ConcurrentDictionary<int, string>> GetAllCodeSkinAsync()
        {
            var list = await (from j in context.CodeSkins select j).ToListAsync();
            foreach (var codeskin in list)
            {
                _dicSkin[codeskin.code] = codeskin.skin;
            }
            return _dicSkin;
        }
        #endregion
        #endregion
    }
}
