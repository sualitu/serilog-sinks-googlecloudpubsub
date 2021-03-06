using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Xunit;
using Google.Pubsub.V1;


namespace Serilog.Sinks.GoogleCloudPubSub.Tests
{

    /// <summary>
    /// Fixture which is set up at the start of the test run, and torn down at the end. 
    /// This creates a new bucket which can be used in all snippets. The bucket is deleted
    /// at the end of the test. The Google Cloud Project name is fetched from the TEST_PROJECT
    /// environment variable
    /// </summary>
    [CollectionDefinition(nameof(GoogleCloudPubsubFixture))]
    public sealed class GoogleCloudPubsubFixture : IDisposable, ICollectionFixture<GoogleCloudPubsubFixture>
    {
        private const string ProjectEnvironmentVariable = "TEST_PROJECT";
        private const string TopicPrefix = "xunit-test-topic-";
        private const string SubscriptionPrefix = "xunit-test-sub-";
        public IConfiguration Config { get; set; }
       
        public string ProjectId { get; }

        public GoogleCloudPubsubFixture()
        {
              var builder = new ConfigurationBuilder()
              .AddEnvironmentVariables();

              Config = builder.Build();

              ProjectId = Config[ProjectEnvironmentVariable];
              Console.WriteLine($"Using projectId [{ProjectId}]");
              if (string.IsNullOrEmpty(ProjectId))
              {
                throw new InvalidOperationException($"Please set the {ProjectEnvironmentVariable} environment variable before running tests");
              }

            var credentials = Config["GOOGLE_APPLICATION_CREDENTIALS"];
            if (string.IsNullOrEmpty(credentials))
            {
                Console.WriteLine($"Using credentials file [{credentials}]");
            }           

         }

        //-------

        internal string GetProjectTopicFull(string topicID)
        {
            return $"projects/{this.ProjectId}/topics/{topicID}";
        }

        internal string GetProjectSubsFull(string subscriptionID)
        {
            return $"projects/{this.ProjectId}/subscriptions/{subscriptionID}";
        }

        //-------

        /// <summary>
        /// Create a topic ID with a prefix which is used to check which topics to delete at the end of the test.
        /// </summary>
        internal string CreateTopicId()
        {
            string newTopicId = TopicPrefix + Guid.NewGuid().ToString().ToLowerInvariant();
            string newTopicIdFull = this.GetProjectTopicFull(newTopicId);

            PublisherClient publisherClient = PublisherClient.Create();
            publisherClient.CreateTopic(newTopicIdFull);

            return newTopicId;
        }

        /// <summary>
        /// Create a subscription ID with a prefix which is used to check which subscriptions to delete at the end of the test.
        /// </summary>
        internal string CreateSubscriptionId(string topicId)
        {
            string newSubsId = SubscriptionPrefix + Guid.NewGuid().ToString().ToLowerInvariant();
            string newSubsIdFull = this.GetProjectSubsFull(newSubsId);
            string topicIdFull = this.GetProjectTopicFull(topicId);

            PushConfig pConfig = new PushConfig();

            SubscriberClient subscriberClient = SubscriberClient.Create();
            subscriberClient.CreateSubscription(newSubsIdFull, topicIdFull, pConfig, 10);

            return newSubsId;
        }

        public void Dispose()
        {
            var subscriber = SubscriberClient.Create();
            var subscriptions = subscriber.ListSubscriptions(SubscriberClient.FormatProjectName(ProjectId))
                .Where(sub => SubscriberClient.SubscriptionTemplate.ParseName(sub.Name)[1].StartsWith(TopicPrefix))
                .ToList();
            foreach (var sub in subscriptions)
            {
                subscriber.DeleteSubscription(sub.Name);
            }

            var publisher = PublisherClient.Create();
            var topics = publisher.ListTopics(PublisherClient.FormatProjectName(ProjectId))
                .Where(topic => PublisherClient.TopicTemplate.ParseName(topic.Name)[1].StartsWith(TopicPrefix))
                .ToList();
            foreach (var topic in topics)
            {
                publisher.DeleteTopic(topic.Name);
            }
        }

      
   }


}