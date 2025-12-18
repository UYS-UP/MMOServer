using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;

public class ExcelDataReaderConverter
{
    /// <summary>
    /// 将Excel文件转换为Dictionary<string, T>
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="filePath">Excel文件路径</param>
    /// <param name="sheetName">工作表名称(可选)</param>
    /// <param name="headerRowIndex">标题行索引(默认为1，即第二行)</param>
    /// <param name="keyColumnIndex">作为字典Key的列索引(默认为0，即第一列)</param>
    /// <returns></returns>
    public static Dictionary<string, T> ConvertToDictionary<T>(
        string filePath,
        string sheetName = null,
        int headerRowIndex = 1,
        int keyColumnIndex = 0) where T : new()
    {
        // 注册编码提供程序(处理中文等编码)
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var dict = new Dictionary<string, T>();

        try
        {
            using (var stream = File.Open(filePath + typeof(T).Name + ".xlsx", FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    // 配置数据集
                    var dataSetConfig = new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                        {
                            // 指定使用标题行
                            UseHeaderRow = true,

                            // 自定义读取标题行的逻辑
                            ReadHeaderRow = (rowReader) =>
                            {
                                // 跳过标题行之前的所有行
                                for (int i = 0; i < headerRowIndex; i++)
                                {
                                    rowReader.Read();
                                }
                            },

                            // 处理空列
                            FilterColumn = (rowReader, columnIndex) =>
                            {
                                return true; // 读取所有列
                            }
                        }
                    };

                    // 将Excel数据转换为DataSet
                    var dataSet = reader.AsDataSet(dataSetConfig);

                    // 获取指定的工作表
                    DataTable dataTable = string.IsNullOrEmpty(sheetName)
                        ? dataSet.Tables[0]
                        : dataSet.Tables[sheetName];

                    if (dataTable == null)
                        throw new ArgumentException($"工作表 '{sheetName}' 不存在");

                    if (dataTable.Rows.Count == 0)
                        return dict; // 空表返回空字典

                    // 验证Key列是否存在
                    if (keyColumnIndex < 0 || keyColumnIndex >= dataTable.Columns.Count)
                        throw new ArgumentOutOfRangeException(nameof(keyColumnIndex), "Key列索引超出范围");

                    // 创建属性映射(列名->属性)
                    var propertyMap = new Dictionary<string, PropertyInfo>();
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        // 查找与列名匹配的属性(不区分大小写)
                        PropertyInfo property = typeof(T).GetProperty(column.ColumnName.Trim(),
                            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                        if (property != null && property.CanWrite)
                        {
                            propertyMap[column.ColumnName] = property;
                        }
                    }

                    // 处理每一行数据
                    foreach (DataRow row in dataTable.Rows)
                    {
                        try
                        {
                            T obj = new T();
                            string key = null;

                            // 处理每一列
                            for (int colIndex = 0; colIndex < dataTable.Columns.Count; colIndex++)
                            {
                                DataColumn column = dataTable.Columns[colIndex];
                                object cellValue = row[column];

                                // 如果是Key列，保存Key值
                                if (colIndex == keyColumnIndex)
                                {
                                    key = cellValue?.ToString();
                                    continue;
                                }

                                // 如果属性映射中存在该列
                                if (propertyMap.TryGetValue(column.ColumnName, out PropertyInfo property))
                                {
                                    try
                                    {
                                        object convertedValue = ConvertValue(cellValue, property.PropertyType);
                                        property.SetValue(obj, convertedValue);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new InvalidOperationException(
                                            $"设置属性 '{property.Name}' 时出错(值: '{cellValue}'): {ex.Message}");
                                    }
                                }
                            }

                            // 确保Key不为空且不重复
                            if (!string.IsNullOrEmpty(key))
                            {
                                if (dict.ContainsKey(key))
                                {
                                    throw new InvalidOperationException($"发现重复的Key值: {key}");
                                }
                                dict[key] = obj;
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException(
                                $"处理行 {dataTable.Rows.IndexOf(row) + headerRowIndex + 1} 时出错: {ex.Message}", ex);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"读取Excel文件 '{filePath}' 时出错: {ex.Message}", ex);
        }

        return dict;
    }

    /// <summary>
    /// 将单元格值转换为目标类型
    /// </summary>
    private static object ConvertValue(object value, Type targetType)
    {
        if (value == null || value == DBNull.Value)
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        // 处理可空类型
        Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // 如果已经是目标类型，直接返回
        if (underlyingType.IsInstanceOfType(value))
        {
            return value;
        }

        // 特殊处理DateTime
        if (underlyingType == typeof(DateTime))
        {
            if (value is double excelDate)
            {
                return DateTime.FromOADate(excelDate);
            }

            if (DateTime.TryParse(value.ToString(), out DateTime dateValue))
            {
                return dateValue;
            }

            return null;
        }
 // 处理枚举类型
    if (underlyingType.IsEnum)
    {
        // 情况1: 值已经是正确的枚举类型
        if (value.GetType() == underlyingType)
        {
            return value;
        }
        
        // 情况2: 值是字符串形式
        if (value is string enumString)
        {
            // 尝试直接解析字符串
            if (Enum.TryParse(underlyingType, enumString, true, out object parsedEnum))
            {
                return parsedEnum;
            }
            
            // 尝试将字符串转为int再转为枚举
            if (int.TryParse(enumString, out int intValue))
            {
                if (Enum.IsDefined(underlyingType, intValue))
                {
                    return Enum.ToObject(underlyingType, intValue);
                }
            }
            
            // 尝试匹配枚举值的ToString()表示
            foreach (var enumValue in Enum.GetValues(underlyingType))
            {
                if (enumValue.ToString().Equals(enumString, StringComparison.OrdinalIgnoreCase))
                {
                    return enumValue;
                }
            }
        }
        
        // 情况3: 值是数值形式(int, double等)
        if (value is IConvertible convertible)
        {
            try
            {
                int intValue = convertible.ToInt32(System.Globalization.CultureInfo.InvariantCulture);
                if (Enum.IsDefined(underlyingType, intValue))
                {
                    return Enum.ToObject(underlyingType, intValue);
                }
            }
            catch
            {
                // 转换失败，继续尝试其他方法
            }
        }
        
        // 情况4: 其他无法识别的格式
        // 返回枚举的默认值(第一个值)
        return Enum.GetValues(underlyingType).GetValue(0);
    }

        // 处理布尔类型
        if (underlyingType == typeof(bool))
        {
            if (value is string boolString)
            {
                return boolString.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
                       boolString.Equals("是", StringComparison.OrdinalIgnoreCase) ||
                       boolString == "1";
            }

            if (value is int intValue)
            {
                return intValue != 0;
            }
        }

        // 默认转换
        try
        {
            return Convert.ChangeType(value, underlyingType);
        }
        catch
        {
            // 如果转换失败，尝试使用ToString()后再转换
            try
            {
                return Convert.ChangeType(value.ToString(), underlyingType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"无法将值 '{value}' ({value.GetType()}) 转换为类型 {underlyingType.Name}: {ex.Message}");
            }
        }
    }
}