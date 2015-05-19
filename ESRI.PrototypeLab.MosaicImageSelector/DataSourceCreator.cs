/*=============================================================================
 * 
 * Copyright © 2010 ESRI. All rights reserved. 
 * 
 * Use subject to ESRI license agreement.
 * 
 * Unpublished—all rights reserved.
 * Use of this ESRI commercial Software, Data, and Documentation is limited to
 * the ESRI License Agreement. In no event shall the Government acquire greater
 * than Restricted/Limited Rights. At a minimum Government rights to use,
 * duplicate, or disclose is subject to restrictions as set for in FAR 12.211,
 * FAR 12.212, and FAR 52.227-19 (June 1987), FAR 52.227-14 (ALT I, II, and III)
 * (June 1987), DFARS 227.7202, DFARS 252.227-7015 (NOV 1995).
 * Contractor/Manufacturer is ESRI, 380 New York Street, Redlands,
 * CA 92373-8100, USA.
 * 
 * SAMPLE CODE IS PROVIDED "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
 * PARTICULAR PURPOSE, ARE DISCLAIMED.  IN NO EVENT SHALL ESRI OR CONTRIBUTORS
 * BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) SUSTAINED BY YOU OR A THIRD PARTY, HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT; STRICT LIABILITY; OR TORT ARISING
 * IN ANY WAY OUT OF THE USE OF THIS SAMPLE CODE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE TO THE FULL EXTENT ALLOWED BY APPLICABLE LAW.
 * 
 * =============================================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace ESRI.PrototypeLab.MosaicImageSelector {
    public static class DataSourceCreator {
        private static readonly Regex PropertNameRegex = new Regex(@"^[A-Za-z]+[A-Za-z1-9_]*$", RegexOptions.Singleline);

        public static IEnumerable ToDataSource(this IEnumerable<IDictionary> list) {
            IDictionary firstDict = null;
            bool hasData = false;
            foreach (IDictionary currentDict in list) {
                hasData = true;
                firstDict = currentDict;
                break;
            }
            if (!hasData) {
                return new object[] { };
            }
            if (firstDict == null) {
                throw new ArgumentException("IDictionary entry cannot be null");
            }

            Type objectType = null;
            TypeBuilder tb = GetTypeBuilder(list.GetHashCode());
            ConstructorBuilder constructor = tb.DefineDefaultConstructor(
                MethodAttributes.Public |
                MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName);

            foreach (DictionaryEntry pair in firstDict) {
                CreateProperty(tb, Convert.ToString(pair.Key), pair.Value == null ? typeof(object) : pair.Value.GetType());
            }
            objectType = tb.CreateType();

            return GenerateEnumerable(objectType, list, firstDict);
        }
        private static IEnumerable GenerateEnumerable(Type objectType, IEnumerable<IDictionary> list, IDictionary firstDict) {
            var listType = typeof(List<>).MakeGenericType(new[] { objectType });
            var listOfCustom = Activator.CreateInstance(listType);
            foreach (var currentDict in list) {
                if (currentDict == null) {
                    throw new ArgumentException("IDictionary entry cannot be null");
                }
                var row = Activator.CreateInstance(objectType);
                foreach (DictionaryEntry pair in firstDict) {
                    if (currentDict.Contains(pair.Key)) {
                        PropertyInfo property = objectType.GetProperty(Convert.ToString(pair.Key));
                        property.SetValue(
                            row,
                            Convert.ChangeType(
                                    currentDict[pair.Key],
                                    property.PropertyType,
                                    null),
                            null);
                    }
                }
                listType.GetMethod("Add").Invoke(listOfCustom, new[] { row });
            }
            return listOfCustom as IEnumerable;
        }
        private static TypeBuilder GetTypeBuilder(int code) {
            AssemblyName an = new AssemblyName("TempAssembly" + code);
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            TypeBuilder tb = moduleBuilder.DefineType("TempType" + code,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout,
                typeof(object));
            return tb;
        }
        private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType) {
            FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);
            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr =
                tb.DefineMethod("get_" + propertyName,
                    MethodAttributes.Public |
                    MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig,
                    propertyType, Type.EmptyTypes);

            ILGenerator getIL = getPropMthdBldr.GetILGenerator();
            getIL.Emit(OpCodes.Ldarg_0);
            getIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getIL.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new Type[] { propertyType });

            ILGenerator setIL = setPropMthdBldr.GetILGenerator();
            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Ldarg_1);
            setIL.Emit(OpCodes.Stfld, fieldBuilder);
            setIL.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }
    }
}
