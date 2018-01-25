using System;
using System.IO;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using slackemoji.img;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace SlackEmoji.Lambda
{
    public class Function
    {
        IAmazonS3 S3Client { get; set; }

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {
            S3Client = new AmazonS3Client();
        }

        /// <summary>
        /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
        /// </summary>
        /// <param name="s3Client"></param>
        public Function(IAmazonS3 s3Client)
        {
            this.S3Client = s3Client;
        }

        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
        /// to respond to S3 notifications.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            var s3Event = evnt.Records?[0].S3;
            if (s3Event == null)
            {
                return;
            }

            try
            {
                Console.WriteLine($"Recieved S3 Event:  {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}");

                // get image from s3
                var image = await S3Client.GetObjectAsync(s3Event.Bucket.Name, s3Event.Object.Key);

                // process image with s3 content stream
                using (image.ResponseStream)
                using (var imgMs = new MemoryStream())
                {
                    image.ResponseStream.CopyTo(imgMs);

                    // resize image
                    ImageResizer imgResizer = new ImageResizer();
                    var outputImage = imgResizer.ResizeImage(imgMs.ToArray());

                    // generate s3 key for new image
                    var destFilenameKey = getS3Key(s3Event.Object.Key);

                    // upload file to s3
                    using (var destMs = new MemoryStream(outputImage))
                    {
                        await S3Client.UploadObjectFromStreamAsync("cb-slack-images", destFilenameKey, destMs, null);
                        Console.WriteLine($"Saved object: {destFilenameKey}");
                    }
                }
            }
            catch (Exception e)
            {
                context.Logger.LogLine(e.Message);
                context.Logger.LogLine(e.StackTrace);
                throw;
            }
        }

        private string getS3Key(string s3Filename)
        {
            // get filename without ext & full path
            int i = s3Filename.IndexOf('/');
            if (i >= 0)
            {
                s3Filename = s3Filename.Substring(i + 1);
            }

            s3Filename = s3Filename.Substring(0, s3Filename.LastIndexOf('.'));

            return $"slack-emojis/{s3Filename}-emoji.png";
        }
    }
}
