using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArdinModel
{
    [Serializable]
    public class IDType<TIDType, TWrapped> : IComparable
        where TIDType : IDType<TIDType, TWrapped>
    {
        private TWrapped value = default(TWrapped);

        public static explicit operator TWrapped(IDType<TIDType, TWrapped> id)
        {
            TWrapped ret = default(TWrapped);

            if (id != null)
            {
                ret = id.value;
            }
            else
            {
                var wrappedType = typeof(TWrapped);
                if (!wrappedType.IsSubclassOf(typeof(Nullable<>)) && (wrappedType != typeof(string)))
                {
                    throw new InvalidOperationException(String.Format("Tried to cast null value to {0}!", wrappedType.Name)); //LOCSTR
                }
                else
                {
                }
            }

            return ret;
        }

        public static TIDType Parse(TWrapped wrap, bool throwExceptionIfNotValid = false)
        {
            TIDType ret = null;
            if (wrap != null)
            {
                ret = Activator.CreateInstance<TIDType>();
                ret.value = wrap;
                IList<string> errors = ret.GetValidationErrors();
                if (errors.Any())
                {
                    if (throwExceptionIfNotValid)
                    {
                        throw new ArgumentException(string.Join(",", errors));
                    }
                    else
                    {
                        ret = null;
                    }
                }
            }

            return ret;
        }

        public static bool operator ==(IDType<TIDType, TWrapped> id1, IDType<TIDType, TWrapped> id2)
        {
            bool ret = false;

            if (Object.ReferenceEquals(id1, id2))
            {
                ret = true;
            }
            else
            {
                if (((object)id1 != null) && ((object)id2 != null))
                {
                    if (Object.Equals(id1.value, id2.value))
                    {
                        ret = true;
                    }
                }
            }

            return ret;
        }

        public static bool operator !=(IDType<TIDType, TWrapped> id1, IDType<TIDType, TWrapped> id2)
        {
            return !(id1 == id2);
        }

        public override bool Equals(object obj)
        {
            bool ret = false;

            IDType<TIDType, TWrapped> other = obj as IDType<TIDType, TWrapped>;
            if (other != null)
            {
                ret = Equals(other);
            }

            return ret;
        }

        public bool Equals(IDType<TIDType, TWrapped> other)
        {
            bool ret = (this == other);
            return ret;
        }

        public override int GetHashCode()
        {
            if (!Object.Equals(this.value, default(TWrapped)))
            {
                return value.GetHashCode();
            }
            else
            {
                return 0;
            }
        }

        public override string ToString()
        {
            if (!Object.Equals(this.value, default(TWrapped)))
            {
                return value.ToString();
            }
            else
            {
                return "no value";
            }
        }

        public virtual IList<string> GetValidationErrors()
        {
            return new List<string>();
        }

        public int CompareTo(object obj)
        {
            int ret = 0;

            if (obj == null)
            {
                ret = 1;
            }
            else
            {
                var otherIDType = obj as IDType<TIDType, TWrapped>;
                if (otherIDType == null)
                {
                    throw new InvalidOperationException(String.Format("Cannot compare type of '{0}' and type of '{1}'", this.GetType(), obj.GetType())); //LOCSTR
                }

                ret = CompareTo(otherIDType);
            }


            return ret;
        }

        public int CompareTo(IDType<TIDType, TWrapped> otherIDType)
        {
            int ret = 0;

            if (otherIDType == null)
            {
                ret = 1;
            }
            else
            {
                if (object.Equals(this.value, default(TWrapped)))
                {
                    if (object.Equals(otherIDType.value, default(TWrapped)))
                    {
                        ret = 0;
                    }
                    else
                    {
                        ret = -1;
                    }
                }
                else
                {
                    if (this.value is IComparable)
                    {
                        ret = ((IComparable)this.value).CompareTo(otherIDType.value);
                    }
                    else
                    {
                        throw new InvalidOperationException(String.Format("Wrapped type '{0}' is not IComparable.", typeof(TWrapped))); //LOCSTR
                    }
                }
            }

            return ret;
        }
    }

}
