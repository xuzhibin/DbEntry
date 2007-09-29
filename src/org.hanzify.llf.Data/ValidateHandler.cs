
#region usings

using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Lephone.Util;
using Lephone.Util.Text;
using Lephone.Data.Common;
using Lephone.Data.Definition;

#endregion

namespace Lephone.Data
{
    public class ValidateHandler
    {
        private string _InvalidFieldText;
        private string _NotAllowNullText;
        private string _NotMatchedText;
        private string _MinLengthText;
        private string _MaxLengthText;
        private string _ShouldBeUniqueText;

        private bool EmptyAsNull;
        private bool IncludeClassName;
        public bool IsValid;

        private Dictionary<string, string> _ErrorMessages;

        public Dictionary<string, string> ErrorMessages
        {
            get { return _ErrorMessages; }
        }

        public ValidateHandler()
            : this(false)
        {
        }

        public ValidateHandler(bool EmptyAsNull)
            : this(EmptyAsNull, false, "Invalid Field {0} {1}.", "Not Allow Null, ", "Not Matched, ", "Min Length is {0}, ", "Max Length is {0}, ", "Should be UNIQUE")
        {
        }

        public ValidateHandler(bool EmptyAsNull, bool IncludeClassName, string InvalidFieldText, string NotAllowNullText, string NotMatchedText, string MinLengthText, string MaxLengthText, string ShouldBeUniqueText)
        {
            this.IsValid = true;
            this.EmptyAsNull = EmptyAsNull;
            this.IncludeClassName = IncludeClassName;

            this._InvalidFieldText = InvalidFieldText;
            this._NotAllowNullText = NotAllowNullText;
            this._NotMatchedText = NotMatchedText;
            this._MinLengthText = MinLengthText;
            this._MaxLengthText = MaxLengthText;
            this._ShouldBeUniqueText = ShouldBeUniqueText;
            
            _ErrorMessages = new Dictionary<string, string>();
        }

        public bool ValidateObject(object obj)
        {
            this.IsValid = true;
            this._ErrorMessages.Clear();

            Type t = obj.GetType();
            ObjectInfo oi = DbObjectHelper.GetObjectInfo(t);
            
            Type StringType = typeof(string);
            foreach (MemberHandler fh in oi.Fields)
            {
                if (fh.FieldType == StringType || (fh.IsLazyLoad && fh.FieldType.GetGenericArguments()[0] == StringType))
                {
                    string Field = fh.IsLazyLoad ? ((LazyLoadField<string>)fh.GetValue(obj)).Value : (string)fh.GetValue(obj);
                    StringBuilder ErrMsg = new StringBuilder();
                    bool isValid = ValidateField(Field, fh, ErrMsg);
                    if (ErrMsg.Length > 2) { ErrMsg.Length -= 2; }
                    if (!isValid)
                    {
                        string n = (IncludeClassName ? t.Name + "." + fh.Name : fh.Name);
                        _ErrorMessages[n] = string.Format(_InvalidFieldText, n, ErrMsg);
                    }
                    this.IsValid &= isValid;
                }
            }
            foreach (List<MemberHandler> mhs in oi.UniqueIndexes.Values)
            {
                WhereCondition c = null;
                string n = "";
                foreach (MemberHandler h in mhs)
                {
                    object v = h.GetValue(obj);
                    if (v != null && v.GetType().IsGenericType)
                    {
                        v = v.GetType().GetField("m_Value", ClassHelper.AllFlag).GetValue(v);
                    }
                    c &= (CK.K[h.Name] == v);
                    n += h.Name;
                }
                if (c != null)
                {
                    if (DbEntry.Context.GetResultCount(t, c) != 0)
                    {
                        this.IsValid = false;
                        if (_ErrorMessages.ContainsKey(n))
                        {
                            _ErrorMessages[n] = string.Format("{0}, {1}", _ErrorMessages[n], _ShouldBeUniqueText);
                        }
                        else
                        {
                            _ErrorMessages[n] = string.Format(_InvalidFieldText, n, _ShouldBeUniqueText);
                        }
                    }
                }
            }
            return this.IsValid;
        }

        private bool ValidateField(string Field, MemberHandler fh, StringBuilder ErrMsg)
        {
            if (Field == null || (Field == "" && EmptyAsNull))
            {
                if (fh.AllowNull)
                {
                    return true;
                }
                else
                {
                    ErrMsg.Append(_NotAllowNullText);
                    return false;
                }
            }
            else
            {
                bool isValid = true;
                Field = Field.Trim();
                if (fh.MaxLength > 0)
                {
                    isValid &= IsValidField(Field, fh.MinLength, fh.MaxLength, !fh.IsUnicode, ErrMsg);
                }
                if (!string.IsNullOrEmpty(fh.Regular))
                {
                    bool iv = Regex.IsMatch(Field, fh.Regular);
                    if (iv)
                    {
                        ErrMsg.Append(_NotMatchedText);
                    }
                    isValid &= iv;
                }
                return isValid;
            }
        }

        private bool IsValidField(string Field, int Min, int Max, bool IsAnsi, StringBuilder ErrMsg)
        {
            int i = IsAnsi ? StringHelper.GetAnsiLength(Field) : Field.Length;

            if (i < Min)
            {
                ErrMsg.Append(string.Format(_MinLengthText, i));
                return false;
            }
            else if (i > Max)
            {
                ErrMsg.Append(string.Format(_MaxLengthText, i));
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
