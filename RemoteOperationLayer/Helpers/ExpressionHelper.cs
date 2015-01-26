using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using System.Collections.Specialized;

namespace ArdinHelpers
{
    public static class ExpressionHelper
    {
        private static ExpressionSerialization.ExpressionSerializer GetSerializer(IEnumerable<Assembly> knownTypeAssemblies)
        {
            ExpressionSerialization.ExpressionSerializer s = new ExpressionSerialization.ExpressionSerializer(
                new ExpressionSerialization.TypeResolver(knownTypeAssemblies), 
                new List<ExpressionSerialization.CustomExpressionXmlConverter>() { new CustomSerializer(knownTypeAssemblies) });
            return s;
        }

        public static XElement Serialize(Expression ex, IEnumerable<Assembly> knownTypeAssemblies)
        {
            if (ex == null)
            {
                throw new ArgumentNullException("ex");
            }
            ExpressionSerialization.ExpressionSerializer s = GetSerializer(knownTypeAssemblies);
            ex = ExpressionSerialization.Evaluator.PartialEval(ex);

            XElement serializedExpression = s.Serialize(ex);

            return serializedExpression;
        }

        public static Expression Deserialize(XElement serializedExpression, IEnumerable<Assembly> knownTypeAssemblies)
        {
            if (serializedExpression == null)
            {
                throw new ArgumentNullException("serializedExpression");
            }

            ExpressionSerialization.ExpressionSerializer s = GetSerializer(knownTypeAssemblies);
            Expression ex = s.Deserialize(serializedExpression);

            return ex;
        }

        public static Expression<Func<TType, bool>> CreatePredicateFromNameValuePairs<TType>(IList<KeyValuePair<string, object>> nameValuePairs, ExpressionType binaryType)
        {
            Expression<Func<TType, bool>> ret = (Expression<Func<TType, bool>>)CreatePredicateFromNameValuePairs(typeof(TType), nameValuePairs, binaryType);
            return ret;
        }

        public static Expression CreatePredicateFromNameValuePairs(Type t, IList<KeyValuePair<string, object>> nameValuePairs, ExpressionType binaryType)
        {
            Expression ret = null;

            ParameterExpression parameterExpression = Expression.Parameter(t, "x");
            Expression leftExpression = null;
            foreach (var nameValuePair in nameValuePairs)
            {
                MemberInfo keyPartProperty = t.GetMember(nameValuePair.Key, BindingFlags.Instance | BindingFlags.Public).Single();
                Expression keyPartPropertyExpression = Expression.MakeMemberAccess(parameterExpression, keyPartProperty);
                if (keyPartPropertyExpression.Type.IsGenericType)
                {
                    if (keyPartPropertyExpression.Type == typeof(Nullable<>).MakeGenericType(keyPartPropertyExpression.Type.GetGenericArguments()))
                    {
                        if (nameValuePair.Value != null)
                        {
                            MemberInfo valueMember = keyPartPropertyExpression.Type.GetMember("Value")[0];
                            keyPartPropertyExpression = Expression.MakeMemberAccess(keyPartPropertyExpression, valueMember);
                        }
                    }
                    else
                    {
                        // trac: https://mecset.ardinsys.hu/projects/ardinvent/trac/ticket/1160
                        throw new NotImplementedException(keyPartPropertyExpression.Type.ToString());
                    }
                }
                Expression keyPartConstantExpression = Expression.Constant(nameValuePair.Value);

                Expression rightExpression = Expression.MakeBinary(binaryType, keyPartPropertyExpression, keyPartConstantExpression);

                if (leftExpression == null)
                {
                    leftExpression = rightExpression;
                }
                else
                {
                    leftExpression = Expression.AndAlso(leftExpression, rightExpression);
                }
            }
            ret = Expression.Lambda(leftExpression, parameterExpression);

            return ret;
        }


        public static object EvaluateExpression(Expression ex, object instance)
        {
            object ret = null;

            if (ex == null)
            {
                throw new ArgumentNullException("ex");
            }

            LambdaExpression lex = ex as LambdaExpression;
            if (lex == null)
            {
                lex = Expression.Lambda(ex);
            }

            if (instance == null)
            {
                ret = lex.Compile().DynamicInvoke();
            }
            else
            {
                ret = lex.Compile().DynamicInvoke(instance);
            }

            return ret;
        }

        public static MethodInfo GetMethodCallExpressionMethodInfo<T>(Expression<Action<T>> exp)
        {
            return GetMethodCallExpressionMethodInfo((Expression)exp);
        }

        public static MethodInfo GetMethodCallExpressionMethodInfo<T>(Expression<Func<T, object>> exp)
        {
            return GetMethodCallExpressionMethodInfo((Expression)exp);
        }

        public static MethodInfo GetMethodCallExpressionMethodInfo(Expression exp)
        {
            MethodInfo ret = null;

            if (exp == null)
            {
                throw new ArgumentNullException("exp");
            }

            MethodCallExpression mcexp = exp as MethodCallExpression;
            if (mcexp == null)
            {
                LambdaExpression lex = exp as LambdaExpression;
                if (lex == null)
                {
                    UnaryExpression uex = exp as UnaryExpression;
                    if ((uex == null) || (uex.NodeType != ExpressionType.Convert))
                    {
                        throw new InvalidOperationException("Neither a MethodCallExpression nor a LambdaExpression wrapped MethodCallExpression nor a ConvertExpression wrapped MethodCallExpression!");   //LOCSTR
                    }
                    else
                    {
                        ret = GetMethodCallExpressionMethodInfo(uex.Operand);
                    }
                }
                else
                {
                    ret = GetMethodCallExpressionMethodInfo(lex.Body);
                }
            }
            else
            {
                ret = mcexp.Method;
            }

            return ret;
        }

        public static IList<string> GetPropertyNamesFromExpressions<TModel>(IList<Expression<Func<TModel, object>>> listOfExpressions)
        {
            return GetPropertyNamesFromExpressions(CreatePropertyExpressions<TModel>(listOfExpressions));
        }

        public static IList<string> GetPropertyNamesFromExpressions(IList<Expression> listOfExpressions)
        {
            List<string> ret = new List<string>();

            if (listOfExpressions == null)
            {
                throw new ArgumentNullException("listOfExpressions"); //LOCSTR
            }

            foreach (Expression ex in listOfExpressions)
            {
                ret.Add(GetPropertyNameFromExpression(ex));
            }

            return ret;
        }

        /// <summary>
        /// Call with 'objectOfTModel => objectOfTModel.OneOfItsProperties', returns "OneOfItsProperties"
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static string GetPropertyNameFromExpression<TModel>(Expression<Func<TModel, object>> expression)
        {
            return GetPropertyNameFromExpression(CreatePropertyExpression<TModel>(expression));
        }

        /// <summary>
        /// Call with 'o => o.OneOfItsProperties', returns "OneOfItsProperties"
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static string GetPropertyNameFromExpression(Expression expression)
        {
            string ret = null;

            PropertyInfo pi = GetPropertyExpressionPropertyInfo(expression);
            ret = pi.Name;

            return ret;
        }

        public static PropertyInfo GetPropertyExpressionPropertyInfo<TModel>(Expression<Func<TModel, object>> expression)
        {
            return GetPropertyExpressionPropertyInfo((Expression)expression);
        }

        public static MemberExpression GetMemberExpression(Expression expression)
        {
            MemberExpression ret = null;

            if (expression == null)
            {
                throw new ArgumentNullException("expression"); //LOCSTR
            }

            LambdaExpression lex = expression as LambdaExpression;
            if (lex != null)
            {
                ret = GetMemberExpression(lex.Body);
            }
            else if (expression.NodeType == ExpressionType.Convert)
            {
                // obj => obj.PropertyOfObj when type of PropertyOfObj is valuetype
                var body = (UnaryExpression)expression;
                ret = GetMemberExpression(body.Operand);
            }
            else
            {
                // obj => obj.PropertyOfObj when type of PropertyOfObj not valuetype
                ret = expression as MemberExpression;
            }

            return ret;
        }

        public static PropertyInfo GetPropertyExpressionPropertyInfo(Expression expression)
        {
            PropertyInfo ret = null;

            MemberExpression mexp = GetMemberExpression(expression);

            if ((mexp == null) || !(mexp.Member is PropertyInfo))
            {
                throw new InvalidOperationException("No MemberExpression with PropertyInfo was found!"); //LOCSTR
            }

            ret = (PropertyInfo)mexp.Member;

            return ret;
        }

        public static IList<Expression> CreatePropertyExpressions<TModel>(IEnumerable<Expression<Func<TModel, object>>> expressions)
        {
            if (expressions == null)
            {
                throw new ArgumentNullException("expressions");
            }

            List<Expression> ret = expressions.Select(ex => (Expression)ex).ToList();

            return ret;
        }


        public static IList<Expression> CreatePropertyExpressions<TModel>(IEnumerable<string> propertyNames)
        {
            List<Expression> ret = new List<Expression>();

            if (propertyNames == null)
            {
                throw new ArgumentNullException("propertyNames"); //LOCSTR
            }

            foreach (string propName in propertyNames)
            {
                ret.Add(CreatePropertyExpression<TModel>(propName));
            }

            return ret;
        }

        /// <summary>
        /// Creates Expression from given lambda.
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static Expression CreatePropertyExpression<TModel>(Expression<Func<TModel, object>> expression)
        {
            return expression;
        }


        /// <summary>
        /// Creates 'obj => obj.PropertyOfObj' style Expression from given property name.
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static LambdaExpression CreatePropertyExpression<TModel>(string propertyName)
        {
            return CreatePropertyExpression(typeof(TModel), propertyName);
        }


        public static LambdaExpression CreatePropertyExpression(Type targetType, string propertyName)
        {
            LambdaExpression ret = null;

            if (String.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException("propertyName");
            }

            ParameterExpression p = Expression.Parameter(targetType, "o");
            MemberExpression m = Expression.Property(p, propertyName);
            Type funcType = typeof(Func<,>).MakeGenericType(targetType, typeof(object));
            if (!m.Type.IsValueType)
            {
                ret = Expression.Lambda(
                    funcType,
                    m,
                    new ParameterExpression[] { p }
                    );
            }
            else
            {
                ret = Expression.Lambda(
                    funcType,
                    Expression.Convert(m, typeof(object)),
                    new ParameterExpression[] { p }
                    );
            }

            return ret;
        }

        public static LambdaExpression CreateMethodCallExpression(Type targetType, string methodName, object[] args)
        {
            LambdaExpression ret = null;

            if (targetType == null)
            {
                throw new ArgumentNullException("targetType");
            }
            if (String.IsNullOrEmpty(methodName))
            {
                throw new ArgumentException("methodName");
            }


            MethodInfo mi = TypeHelper.GetMethodInfo(targetType, methodName, args);

            var methodParameterInfos = mi.GetParameters();
            var methodCallArguments = new List<Expression>();
            for(int i=0; i < methodParameterInfos.Length; i++)
            {
                Expression methodCallArgument = Expression.Constant(args[i], methodParameterInfos[i].ParameterType);
                methodCallArguments.Add(methodCallArgument);
            }

            ParameterExpression p = Expression.Parameter(targetType, "o");
            MethodCallExpression m = Expression.Call(p, mi, methodCallArguments.ToArray());

            ret = Expression.Lambda(m, p);

            return ret;
        }

        public static LambdaExpression CreateMethodCallExpression(Type baseType, string propertyName, Type targetType, string methodName, object[] args)
        {
            LambdaExpression ret = null;

            if (targetType == null)
            {
                throw new ArgumentNullException("targetType");
            }
            if (String.IsNullOrEmpty(methodName))
            {
                throw new ArgumentException("methodName");
            }


            MethodInfo mi = TypeHelper.GetMethodInfo(targetType, methodName, args);

            var methodParameterInfos = mi.GetParameters();
            var methodCallArguments = new List<Expression>();
            for (int i = 0; i < methodParameterInfos.Length; i++)
            {
                Expression methodCallArgument = Expression.Constant(args[i], methodParameterInfos[i].ParameterType);
                methodCallArguments.Add(methodCallArgument);
            }

            ParameterExpression p = Expression.Parameter(baseType, "o");
            MemberExpression mx = Expression.Property(p, propertyName);
            MethodCallExpression m = Expression.Call(mx, mi, methodCallArguments.ToArray());

            ret = Expression.Lambda(m, p);

            return ret;
        }


        /// <summary>
        /// Replaces types in expression.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="typeReplacements"></param>
        /// <returns></returns>
        public static Expression ReplaceTypes(Expression e, Dictionary<Type, Type> typeReplacements)
        {
            LambdaExpression lex = e as LambdaExpression;
            if (lex == null)
            {
                throw new ArgumentException("Should be called with LambdaExpression!"); //LOCSTR
            }

            var converter = new TypeReplacerVisitor(typeReplacements);
            Expression ret = converter.Visit(lex);

            return ret;
        }



        /// <summary>
        /// Based on: http://stackoverflow.com/questions/4601844/expression-tree-copy-or-convert
        /// </summary>
        private class TypeReplacerVisitor : ExpressionVisitor
        {
            private Dictionary<Type, Type> typeReplacements = null;
            private Dictionary<string, ParameterExpression> createdParameterExpressions = null;

            public TypeReplacerVisitor(Dictionary<Type, Type> typeReplacements)
            {
                this.typeReplacements = typeReplacements;
                this.createdParameterExpressions = new Dictionary<string, ParameterExpression>();
            }

            private ParameterExpression GetParameter(Type parameterType, string parameterName)
            {
                ParameterExpression ret = null;
                if (!createdParameterExpressions.TryGetValue(parameterName, out ret))
                {
                    ret = Expression.Parameter(parameterType, parameterName);
                    createdParameterExpressions.Add(parameterName, ret);
                }

                return ret;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                Type replacementType = null;
                if (typeReplacements.TryGetValue(node.Type, out replacementType))
                {
                    return GetParameter(replacementType, node.Name);
                }
                else
                {
                    return node;
                }
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                Expression ret = null;

                var newObj = Visit(node.Expression);
                var newMember = newObj.Type.GetMember(node.Member.Name).First();
                ret = Expression.MakeMemberAccess(newObj, newMember);

                return ret;
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                var newBody = Visit(node.Body);

                List<ParameterExpression> newParameters = new List<ParameterExpression>();
                foreach (var p in node.Parameters)
                {
                    Type replacementType = p.Type;
                    if (typeReplacements.TryGetValue(p.Type, out replacementType))
                    {
                        newParameters.Add(GetParameter(replacementType, p.Name));
                    }
                    else
                    {
                        newParameters.Add(p);
                    }
                }

                Expression newLambda = Expression.Lambda(newBody, newParameters);
                return newLambda;
            }
        }



        /// <summary>
        /// Replaces parameters in expression.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="oldParameter"></param>
        /// <param name="newParameter"></param>
        /// <returns></returns>
        public static Expression ReplaceParameters(Expression expression, ParameterExpression source, ParameterExpression target)
        {
            return new ParameterReplacerVisitor2(source, target).Visit(expression);
        }



        private class ParameterReplacerVisitor2 : ExpressionVisitor
        {
            private ParameterExpression _source;
            private ParameterExpression _target;

            public ParameterReplacerVisitor2(ParameterExpression source, ParameterExpression target)
            {
                _source = source;
                _target = target;
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                Expression ret = null;

                // Leave all parameters alone except the one we want to replace.
                var parameters = node.Parameters.Where(p => p != _source).ToList();
                if (parameters.Count != node.Parameters.Count)
                {
                    // lambda had a replaceable parameter

                    if (!parameters.Contains(_target))
                    {
                        parameters.Add(_target);
                    }

                    ret = Expression.Lambda(Visit(node.Body), parameters);
                }
                else
                {
                    ret = base.VisitLambda<T>(node);
                }

                return ret;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                // Replace the source with the target, visit other params as usual.
                Expression ret = node == _source ? _target : base.VisitParameter(node);
                return ret;
            }
        }







        /// <summary>
        /// ExpressionSerialization cannot deal with nonprimitive types in ConstantExpression's value.
        /// This CustomExpressionXmlConverter will handle this problem.
        /// </summary>
        private class CustomSerializer : ExpressionSerialization.CustomExpressionXmlConverter
        {
            private const string ELEMENTMARKER = "CustomConstantExpression";
            private const string TYPEMARKER = "Type";
            private const string VALUEMARKER = "Value";

            private IEnumerable<Assembly> knownTypeAssemblies = null;

            public CustomSerializer(IEnumerable<Assembly> knownTypeAssemblies)
            {
                this.knownTypeAssemblies = knownTypeAssemblies;
            }
            
            public override bool TryDeserialize(XElement expressionXml, out Expression e)
            {
                e = null;
                bool ret = false;

                if (expressionXml.Name == ELEMENTMARKER)
                {
                    string typeString = expressionXml.Element(TYPEMARKER).Value;
                    Type type = Type.GetType(typeString);
                    if (type == null)
                    {
                        throw new InvalidOperationException(String.Format("Cannot find type: {0}", typeString)); //LOCSTR
                    }
                    string valueString = expressionXml.Element(VALUEMARKER).Value;
                    object value = SerializationHelper.DeserializeFromXml(valueString);

                    e = Expression.Constant(value, type);
                    ret = true;
                }

                return ret;
            }

            public override bool TrySerialize(Expression expression, out XElement x)
            {
                x = null;
                bool ret = false;

                ConstantExpression cex = expression as ConstantExpression;
                if (cex != null)
                {
                    List<XObject> xElementObjects = new List<XObject>();

                    xElementObjects.Add(new XElement(TYPEMARKER, cex.Type.AssemblyQualifiedName));
                    xElementObjects.Add(new XElement(VALUEMARKER, SerializationHelper.SerializeToXml(cex.Value)));

                    string xName = ELEMENTMARKER;

                    x = new XElement(xName, xElementObjects.ToArray());
                    ret = true;

                }

                return ret;
            }
        }


        public static Expression PartialEval(Expression ex)
        {
            Expression ret = ExpressionEvaluator.PartialEval(ex);
            return ret;
        }


        private static class ExpressionEvaluator
        {
            /// <summary>
            /// Performs evaluation & replacement of independent sub-trees
            /// </summary>
            /// <param name="expression">The root of the expression tree.</param>
            /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
            /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
            public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
            {
                return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);
            }

            /// <summary>
            /// Performs evaluation & replacement of independent sub-trees
            /// </summary>
            /// <param name="expression">The root of the expression tree.</param>
            /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
            public static Expression PartialEval(Expression expression)
            {
                return PartialEval(expression, ExpressionEvaluator.CanBeEvaluatedLocally);
            }

            private static bool CanBeEvaluatedLocally(Expression expression)
            {
                return expression.NodeType != ExpressionType.Parameter;
            }

            /// <summary>
            /// Evaluates & replaces sub-trees when first candidate is reached (top-down)
            /// </summary>
            class SubtreeEvaluator : ExpressionVisitor
            {
                HashSet<Expression> candidates;

                internal SubtreeEvaluator(HashSet<Expression> candidates)
                {
                    this.candidates = candidates;
                }

                internal Expression Eval(Expression exp)
                {
                    return this.Visit(exp);
                }

                public override Expression Visit(Expression exp)
                {
                    if (exp == null)
                    {
                        return null;
                    }
                    if (this.candidates.Contains(exp))
                    {
                        return this.Evaluate(exp);
                    }
                    return base.Visit(exp);
                }

                private Expression Evaluate(Expression e)
                {
                    if (e.NodeType == ExpressionType.Constant)
                    {
                        return e;
                    }
                    LambdaExpression lambda = Expression.Lambda(e);
                    Delegate fn = lambda.Compile();
                    return Expression.Constant(fn.DynamicInvoke(null), e.Type);
                }
            }

            /// <summary>
            /// Performs bottom-up analysis to determine which nodes can possibly
            /// be part of an evaluated sub-tree.
            /// </summary>
            private class Nominator : ExpressionVisitor
            {
                Func<Expression, bool> fnCanBeEvaluated;
                HashSet<Expression> candidates;
                bool cannotBeEvaluated;

                internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
                {
                    this.fnCanBeEvaluated = fnCanBeEvaluated;
                }

                internal HashSet<Expression> Nominate(Expression expression)
                {
                    this.candidates = new HashSet<Expression>();
                    this.Visit(expression);
                    return this.candidates;
                }

                public override Expression Visit(Expression expression)
                {
                    if (expression != null)
                    {
                        bool saveCannotBeEvaluated = this.cannotBeEvaluated;
                        this.cannotBeEvaluated = false;
                        base.Visit(expression);
                        if (!this.cannotBeEvaluated)
                        {
                            if (this.fnCanBeEvaluated(expression))
                            {
                                this.candidates.Add(expression);
                            }
                            else
                            {
                                this.cannotBeEvaluated = true;
                            }
                        }
                        this.cannotBeEvaluated |= saveCannotBeEvaluated;
                    }
                    return expression;
                }
            }
        }

        public static LambdaExpression CreatePropertySetValueExpression(Type targetType, string propertyName, object value)
        {
            LambdaExpression ret = null;

            if (String.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException("propertyName");
            }

            ParameterExpression p = Expression.Parameter(targetType, "o");
            MemberExpression m = Expression.Property(p, propertyName);

            Expression assignbody;
            if (m.Type == value.GetType())
            {
                assignbody = Expression.Constant(value);
            }
            else
            {
                assignbody = Expression.Convert(Expression.Constant(value), m.Type);
            }
            BinaryExpression assign = Expression.Assign(m, assignbody);

            Type funcType = typeof(Action<>).MakeGenericType(targetType);
            if (!m.Type.IsValueType)
            {
                ret = Expression.Lambda(
                    funcType,
                    assign,
                    new ParameterExpression[] { p }
                    );
            }
            else
            {
                ret = Expression.Lambda(
                    funcType,
                    assign,
                    new ParameterExpression[] { p }
                    );
            }

            return ret;
        }

        public static Expression<Func<TParam, TRet>> JoinExpressions<TParam, TRet>(ExpressionType expressionType, params Expression<Func<TParam, TRet>>[] expressions)
        {
            return JoinExpressions<TParam, TRet>(expressions.ToList(), expressionType);
        }

        /// <summary>
        /// Joins expressions into one expression using expressionType operator between them.
        /// Expression parameters will be standardized.
        /// E.g.:   
        ///     ({ cib=>cib.Cikk == c, cikkInfoBase=>cikkInfoBase.AlimedKod == "1234"}, AndAlso) 
        /// results:
        ///     cib=>cib.Cikk == c AndAlso cib.AlimedKod == "1234"
        /// </summary>
        public static Expression<Func<TParam, TRet>> JoinExpressions<TParam, TRet>(List<Expression<Func<TParam, TRet>>> expressions, ExpressionType expressionType)
        {
            Expression<Func<TParam, TRet>> ret = (Expression<Func<TParam, TRet>>)JoinExpressions(expressions.Cast<LambdaExpression>().ToList(), expressionType);
            return ret;
        }

        public static LambdaExpression JoinExpressions(List<LambdaExpression> expressions, ExpressionType expressionType)
        {
            LambdaExpression ret = null;

            if (expressions != null)
            {
                if (expressions.Any())
                {
                    ret = expressions.First();
                }

                foreach (var expr in expressions.Skip(1))
                {
                    Expression joinedBody = Expression.MakeBinary(expressionType, ret.Body, expr.Body);
                    Expression joined = Expression.Lambda(joinedBody, ret.Parameters[0]);
                    ret = (LambdaExpression)ReplaceParameters(joined, expr.Parameters[0], ret.Parameters[0]);
                }
            }

            return ret;
        }

        public static Expression Clone(Expression expression)
        {
            Expression ret = null;

            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            string serialized = SerializationHelper.SerializeToXml(expression);
            ret = (Expression)SerializationHelper.DeserializeFromXml(serialized);

            return ret;
        }
    }
}
