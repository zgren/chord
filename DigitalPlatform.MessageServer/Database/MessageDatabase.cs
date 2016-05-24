﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

using DigitalPlatform.Text;

namespace DigitalPlatform.MessageServer
{
    // 消息 数据库
    public class MessageDatabase : MongoDatabase<MessageItem>
    {
        public override async Task CreateIndex()
        {
            if (_collection == null)
                throw new Exception("_collection 尚未初始化");

            await _collection.Indexes.CreateOneAsync(
                Builders<MessageItem>.IndexKeys.Ascending("publishTime"),
                new CreateIndexOptions() { Unique = false });
            await _collection.Indexes.CreateOneAsync(
    Builders<MessageItem>.IndexKeys.Ascending("creator"),
    new CreateIndexOptions() { Unique = false });
            await _collection.Indexes.CreateOneAsync(
Builders<MessageItem>.IndexKeys.Ascending("groups"),
new CreateIndexOptions() { Unique = false });
            await _collection.Indexes.CreateOneAsync(
Builders<MessageItem>.IndexKeys.Ascending("thread"),
new CreateIndexOptions() { Unique = false });
        }

        // return:
        //      true    表示后面要继续处理
        //      false 表示后面要中断处理
        public delegate bool Delegate_outputMessage(long totalCount, MessageItem item);

        // 返回空表示任意匹配
        FilterDefinition<MessageItem> BuildQuery(GroupQuery group_query,
            string timeRange)
        {
            string strStart = "";
            string strEnd = "";
            StringUtil.ParseTwoPart(timeRange, "~", out strStart, out strEnd);
            DateTime startTime;
            DateTime endTime;
            try
            {
                startTime = string.IsNullOrEmpty(strStart) ? new DateTime(0) : DateTime.Parse(strStart);
                endTime = string.IsNullOrEmpty(strEnd) ? new DateTime(0) : DateTime.Parse(strEnd);
            }
            catch (Exception)
            {
                throw new ArgumentException("时间范围字符串 '" + timeRange + "' 不合法", "timeRange");
            }

            FilterDefinition<MessageItem> time_filter = null;
            if (startTime == new DateTime(0) && endTime == new DateTime(0))
                time_filter = null;  // Builders<MessageItem>.Filter.Gte("publishTime", startTime);
            else if (startTime == new DateTime(0))
                time_filter = Builders<MessageItem>.Filter.Lt("publishTime", endTime);
            else if (endTime == new DateTime(0))
                time_filter = Builders<MessageItem>.Filter.Gte("publishTime", startTime);
            else
            {
                time_filter = Builders<MessageItem>.Filter.And(
Builders<MessageItem>.Filter.Gte("publishTime", startTime),
Builders<MessageItem>.Filter.Lt("publishTime", endTime));
            }

            FilterDefinition<MessageItem> expire_filter = Builders<MessageItem>.Filter.Or(
Builders<MessageItem>.Filter.Eq("expireTime", new DateTime(0)),
Builders<MessageItem>.Filter.Gt("expireTime", DateTime.Now));

            // 构造一个 AND 运算的检索式
            FilterDefinition<MessageItem> group_filter = group_query.BuildMongoQuery();

            if (time_filter == null)
                return Builders<MessageItem>.Filter.And(expire_filter,
                group_filter);

            return time_filter = Builders<MessageItem>.Filter.And(time_filter,
                expire_filter,
                group_filter);
        }

        // parameters:
        //      timeRange   时间范围
        public async Task GetMessages(// string groupName,
            GroupQuery group_query,
            string timeRange,
int start,
int count,
            Delegate_outputMessage proc)
        {
            IMongoCollection<MessageItem> collection = this._collection;

            // List<MessageItem> results = new List<MessageItem>();
            FilterDefinition<MessageItem> filter = BuildQuery(// groupName,
                group_query,
                timeRange);
#if NO
            if (string.IsNullOrEmpty(groupName))
            {
                filter = Builders<MessageItem>.Filter.Or(
                    Builders<MessageItem>.Filter.Eq("group", ""),
                    Builders<MessageItem>.Filter.Eq("group", (string)null));
            }
            else
#endif
            // filter = Builders<MessageItem>.Filter.Eq("group", groupName);

            var sort = Builders<MessageItem>.Sort.Ascending("publishTime");
            var options = new FindOptions<MessageItem, MessageItem> { Sort = sort };

            long totalCount = 0;
            var index = 0;
            using (var cursor = await collection.FindAsync(
                filter == null ? new BsonDocument() : filter
                , options))
            {

                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    int batch_count = batch.Count<MessageItem>();
                    Console.WriteLine("batch.Count=" + totalCount);
                    foreach (var document in batch)
                    {
                        if (count != -1 && index - start >= count)
                            break;
                        if (index >= start)
                        {
                            if (proc(-2, document) == false)    // -2 表示总记录数暂时未知。发送全部结束的时候会单独一次发出总记录数
                                return;
                        }
                        index++;
                    }
                    totalCount += batch_count;
                }
                proc(totalCount, null); // 表示结束
            }

        }

        // 注意，失效的消息也返回了
        public async Task<List<MessageItem>> GetMessages(string groupName,
    int start,
    int count)
        {
            IMongoCollection<MessageItem> collection = this._collection;

            List<MessageItem> results = new List<MessageItem>();

            var filter = Builders<MessageItem>.Filter.Eq("group", groupName);
            var index = 0;
            using (var cursor = await collection.FindAsync(
                groupName == "*" ? new BsonDocument() : filter
                ))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    foreach (var document in batch)
                    {
                        if (count != -1 && index - start >= count)
                            break;
                        if (index >= start)
                            results.Add(document);
                        index++;
                    }
                }
            }

            return results;
        }

        public async Task<List<MessageItem>> GetMessageByID(string id,
    int start = 0,
    int count = -1)
        {
            IMongoCollection<MessageItem> collection = this._collection;

            List<MessageItem> results = new List<MessageItem>();

            var filter = Builders<MessageItem>.Filter.Eq("id", id);
            var index = 0;
            using (var cursor = await collection.FindAsync(filter))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    foreach (var document in batch)
                    {
                        if (count != -1 && index - start >= count)
                            break;
                        if (index >= start)
                            results.Add(document);
                        index++;
                    }
                }
            }

            return results;
        }

#if NO
        // 检索出指定范围的 群名类型
        public async Task GetGroups(
    GroupQuery group_query,
    string timeRange,
int start,
int count,
    Delegate_outputMessage proc)
        {
            IMongoCollection<MessageItem> collection = this._collection;

            // List<MessageItem> results = new List<MessageItem>();
            FilterDefinition<MessageItem> filter = BuildQuery(// groupName,
                group_query,
                timeRange);

            var myresults = await collection.Aggregate()
.Group(new BsonDocument("_id", "$groups"))
.ToListAsync();

#if NO
            BsonArray array = new BsonArray();
            array.ToArray<string>();
#endif
            long totalCount = myresults.Count;
            foreach (BsonDocument doc in myresults)
            {
                MessageItem item = new MessageItem();
                BsonArray array = (doc.GetValue("_id") as BsonArray);
                item.groups = GetStringArray(array);
                // var groups = doc.GetValue("_id");

                if (proc(totalCount, item) == false)
                    return;
            }

            proc(totalCount, null); // 表示结束
        }

#endif

#if NO
        // 检索出指定范围的 群名类型
        public async Task GetGroups(
    GroupQuery group_query,
    string timeRange,
int start,
int count,
    Delegate_outputMessage proc)
        {
            IMongoCollection<MessageItem> collection = this._collection;

            // List<MessageItem> results = new List<MessageItem>();
            FilterDefinition<MessageItem> filter = BuildQuery(// groupName,
                group_query,
                timeRange);

            var results = await collection                
                .Find(
                filter == null ? new BsonDocument() : filter
                )
                .Project(Builders<MessageItem>.Projection.Include("groups")).ToListAsync();

            List<string> keys = new List<string>();
            Hashtable table = new Hashtable();  // groups --> true 
            foreach (BsonDocument doc in results)
            {
                string strText = ToString(doc.GetValue("groups") as BsonArray);
                if (strText == null)
                    continue;
                if (table.ContainsKey(strText))
                    continue;
                table[strText] = true;
                keys.Add(strText);
            }

            long totalCount = keys.Count;
            foreach(string key in keys)
            {
                MessageItem item = new MessageItem();
                item.groups = key.Split(new char [] {','});
                // var groups = doc.GetValue("_id");

                if (proc(totalCount, item) == false)
                    return;
            }

            proc(totalCount, null); // 表示结束

#if NO
            var myresults = await collection.Aggregate()
.Group(new BsonDocument("_id", "$groups"))
.ToListAsync();

            long totalCount = myresults.Count;
            foreach (BsonDocument doc in myresults)
            {
                MessageItem item = new MessageItem();
                BsonArray array = (doc.GetValue("_id") as BsonArray);
                item.groups = GetStringArray(array);
                // var groups = doc.GetValue("_id");

                if (proc(totalCount, item) == false)
                    return;
            }

            proc(totalCount, null); // 表示结束
#endif
        }

#endif

        // 这个版本速度最快。因为 Group 操作是在 mongodb 数据库内执行的
        // 检索出指定范围的 群名类型
        // Aggregate() 如何与 filter 一起使用
        // http://stackoverflow.com/questions/29804225/mongodb-driver-2-0-c-sharp-filter-and-aggregate
        public async Task GetGroupsFieldAggragate(
    GroupQuery group_query,
    string timeRange,
int start,
int count,
    Delegate_outputMessage proc)
        {
            IMongoCollection<MessageItem> collection = this._collection;

            FilterDefinition<MessageItem> filter = BuildQuery(// groupName,
                group_query,
                timeRange);

            var myresults = await collection.Aggregate().Match(filter)
.Group(new BsonDocument("_id", "$groups"))
.ToListAsync();

            long totalCount = myresults.Count;
            var index = 0;
            foreach (BsonDocument doc in myresults)
            {
                if (count != -1 && index - start >= count)
                    break;
                if (index >= start)
                {
                    MessageItem item = new MessageItem();
                    BsonArray array = (doc.GetValue("_id") as BsonArray);
                    item.groups = GetStringArray(array);

                    if (proc(totalCount, item) == false)
                        return;
                }

                index++;
            }

            proc(totalCount, null); // 表示结束
        }

        // 这个版本资源耗费厉害
        // 按照条件检索出 MessageItem 中的 group 字段，并归并去重
        // 相当于 Group by 的效果
        public async Task GetGroupsField(
    GroupQuery group_query,
    string timeRange,
int start,
int count,
    Delegate_outputMessage proc)
        {
            IMongoCollection<MessageItem> collection = this._collection;

            // List<MessageItem> results = new List<MessageItem>();
            FilterDefinition<MessageItem> filter = BuildQuery(// groupName,
                group_query,
                timeRange);

            // 在遍历过程中，只接收 groups 字段
            // http://stackoverflow.com/questions/32938656/c-sharp-mongo-2-0-reduce-traffic-of-findasync
            var projection = Builders<MessageItem>.Projection
    .Include(b => b.groups)
    .Exclude("_id"); // _id is special and needs to be explicitly excluded if not needed
            var options = new FindOptions<MessageItem, MessageItem> { Projection = projection };

            List<string> keys = new List<string>();
            Hashtable table = new Hashtable();  // groups --> true 

            using (var cursor = await collection.FindAsync(filter, options))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    foreach (var document in batch)
                    {
                        if (document.groups == null)
                            continue;
                        string strText = string.Join(",", document.groups);
                        if (table.ContainsKey(strText))
                            continue;
                        table[strText] = true;
                        keys.Add(strText);
                    }
                }
            }

            long totalCount = keys.Count;
            var index = 0;
            foreach (string key in keys)
            {
                if (count != -1 && index - start >= count)
                    break;
                if (index >= start)
                {
                    MessageItem item = new MessageItem();
                    item.groups = key.Split(new char[] { ',' });
                    if (proc(totalCount, item) == false)
                        return;
                }
                index++;
            }

            proc(totalCount, null); // 表示结束
        }

        static string ToString(BsonArray array)
        {
            if (array == null)
                return null;

            StringBuilder text = new StringBuilder();
            foreach (BsonValue v in array)
            {
                if (text.Length > 0)
                    text.Append(",");
                text.Append(v.ToString());
            }
            return text.ToString();
        }

        static string[] GetStringArray(BsonArray array)
        {
            if (array == null)
                return null;

            List<string> results = new List<string>();
            foreach (BsonValue v in array)
            {
                results.Add(v.ToString());
            }
            return results.ToArray();
        }

        // parameters:
        //      item    要加入的消息事项
        public async Task Add(MessageItem item)
        {
            // 检查 item
            if (string.IsNullOrEmpty(item.creator) == true)
                throw new Exception("creator 不能为空");

            // 规范化数据

            // group 的空实际上代表一个群组
            if (item.groups == null)
                item.groups = new string[1] { "" };
            else
                Array.Sort(item.groups);    // 排序后确保名字规范，将来用起来(比如构造为逗号间隔的字符串时)可以少一次排序

            IMongoCollection<MessageItem> collection = this._collection;

            //item.publishTime = DateTime.Now;
            //item.expireTime = new DateTime(0); // 表示永远不失效

            await collection.InsertOneAsync(item);
        }

        // 更新 id,groups,expireTime 以外的全部字段
        public async Task Update(MessageItem item)
        {
            // 检查 item
            if (string.IsNullOrEmpty(item.id) == true)
                throw new Exception("id 不能为空");

            IMongoCollection<MessageItem> collection = this._collection;

            // var filter = Builders<UserItem>.Filter.Eq("id", item.id);
            var filter = Builders<MessageItem>.Filter.Eq("id", item.id);
            var update = Builders<MessageItem>.Update
                // .Set("groups", item.groups)
                .Set("creator", item.creator)
                .Set("userName", item.userName)
                .Set("data", item.data)
                .Set("format", item.format)
                .Set("type", item.type)
                .Set("thread", item.thread);

            await collection.UpdateOneAsync(filter, update);
        }

        // 根据一个字段的特征删除匹配的事项
        public async Task Delete(string field, string value)
        {
            IMongoCollection<MessageItem> collection = this._collection;

            // var filter = Builders<UserItem>.Filter.Eq("id", item.id);
            var filter = Builders<MessageItem>.Filter.Eq(field, value);

            await collection.DeleteOneAsync(filter);
        }

        // 根据一个字段的特征，立即失效匹配的事项
        // parameters:
        //      now 用于修改 expireTime 字段的时间
        public async Task Expire(string field, string value, DateTime now)
        {
            IMongoCollection<MessageItem> collection = this._collection;

            var filter = Builders<MessageItem>.Filter.Eq(field, value);

            var update = Builders<MessageItem>.Update
                .Set("expireTime", now);

            await collection.UpdateOneAsync(filter, update);
        }

        public async Task DeleteByID(string id)
        {
            // 检查 id
            if (string.IsNullOrEmpty(id) == true)
                throw new ArgumentException("id 不能为空");

            await Delete("id", id);
        }

        public async Task ExpireByID(string id, DateTime now)
        {
            // 检查 id
            if (string.IsNullOrEmpty(id) == true)
                throw new ArgumentException("id 不能为空");

            await Expire("id", id, now);
        }
    }

    public class MessageItem
    {
        public void SetID(string id)
        {
            this.id = id;
        }

        [BsonId]    // 允许 GUID
        public string id { get; private set; }  // 消息的 id

        public string[] groups { get; set; }   // 组名 或 组id。消息所从属的组
        public string creator { get; set; } // 创建消息的人的id
        public string userName { get; set; } // 创建消息的人的用户名
        public string data { get; set; }  // 消息数据体
        public string format { get; set; } // 消息格式。格式是从存储格式角度来说的
        public string type { get; set; }    // 消息类型。类型是从用途角度来说的
        public string thread { get; set; }    // 消息所从属的话题线索

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime publishTime { get; set; } // 消息发布时间

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime expireTime { get; set; } // 消息失效时间

        // TODO: 消息的历次修改者和时间。也可以不采用这种数据结构，而是在修改后在原时间重新写入一条修改后消息，并注明前后沿革关系
    }

}
