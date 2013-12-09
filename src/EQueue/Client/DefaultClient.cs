﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using EQueue.Client.Consumer;
using EQueue.Client.Producer;
using EQueue.Common;
using EQueue.Common.Logging;

namespace EQueue.Client
{
    public class DefaultClient
    {
        private readonly ConcurrentDictionary<string, IProducer> _producerDict = new ConcurrentDictionary<string, IProducer>();
        private readonly ConcurrentDictionary<string, IConsumer> _consumerDict = new ConcurrentDictionary<string, IConsumer>();
        private readonly ConcurrentDictionary<string, TopicRouteData> _topicRouteDataDict = new ConcurrentDictionary<string, TopicRouteData>();
        private readonly ILogger _logger;
        private readonly ClientConfig _config;
        private Timer _timer;

        public string ClientId { get; private set; }
        public IPullMessageService PullMessageService { get; private set; }

        public DefaultClient(string clientId, ClientConfig config, IPullMessageService pullMessageService, ILoggerFactory loggerFactory)
        {
            ClientId = clientId;
            _config = config;
            PullMessageService = pullMessageService;
            _logger = loggerFactory.Create(GetType().Name);
            _logger.InfoFormat("A new mq client create, ClinetID: {0}, Config:{1}", ClientId, _config);
        }

        public void Start()
        {
            PullMessageService.Start();
            StartRebalance();
        }

        public IConsumer SelectConsumer(string consumerGroup)
        {
            IConsumer consumer;
            if (_consumerDict.TryGetValue(consumerGroup, out consumer))
            {
                return consumer;
            }
            return null;
        }
        public IEnumerable<string> FindConsumerIdList(string consumerGroup)
        {
            //TODO
            return null;
        }


        private void StartRebalance()
        {
            if (_timer == null)
            {
                _timer = new Timer((obj) =>
                {
                    foreach (var consumer in _consumerDict.Values)
                    {
                        try
                        {
                            consumer.DoRebalance();
                        }
                        catch (Exception ex)
                        {
                            _logger.Error("Rebalance has exception.", ex);
                        }
                    }
                }, null, 1000 * 10, 1000 * 10);
            }
        }
    }
}