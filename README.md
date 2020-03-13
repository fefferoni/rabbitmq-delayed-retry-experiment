# rabbitmq-delayed-retry-experiment
Experimenting with RabbitMQ to implement a worker queue with a limited number of retries and an exponential back-off.
The image below represents a flow diagram of a scenario that one might want to accomplish by implementing a message queue solution like the one in this experiment.

<b>Note:</b> this is merely a quick sample on how to use RabbitMQ for a certain scenario. Therefore I have not put any effort into using any certain coding principles or patterns.

The repo consists of two separate .Net Core Console Apps that, when run separately, communicate with each other via RabbitMQ. The <b>Producer</b> is responsible for creating and dispatching initial messages to the message broker and the <b>Consumer</b> is responsible to receive and process the messages. Please check out the tutorials on rabbitmq.com on how to install the broker server.

In this experiment, I have the Producer pull five random strings from some web api and put them as messages on a main queue. The Consumer subscribes to this queue and when it receives a message containing a string shorter than a certain length it pretends this was a failure in processing the message. A failed message gets it's expiration set and is sent to the retry queue. 

The retry queue has it's dead lettering queue set to the main queue. Since no Consumer is subscribing to the retry queue the messages just sit here until they expire and RabbitMQ puts them back to the main queue.

<img alt="Flow diagram overview" src="https://github.com/fefferoni/rabbitmq-delayed-retry-experiment/blob/master/Untitled%20Diagram.png">
