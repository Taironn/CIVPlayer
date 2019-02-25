import os
import json
import boto3
from datetime import datetime, timezone


def handler(event, context):
    try:
        client = boto3.client('dynamodb')
        body = get_body(event)
        client.put_item(
            TableName=os.environ['TableName'],
            Item={
                'username': {
                    'S': body['username']
                },
                'timestamp': {
                    'S': datetime.now(timezone.utc).isoformat()
                }

            }
        )

        response = {
            "statusCode": 200,
        }
        return response
    except Exception as e:
        print(e)
        return {
            "statusCode": 400
        }


def get_body(event):
    return json.loads(event['body'])
