import os
import json
import boto3
from datetime import datetime, timezone


def handler(event, context):
    try:
        body = get_body(event)
        db = boto3.resource('dynamodb')
        table = db.Table(os.environ['TableName'])
        table.put_item(
            Item={
                'username': body['username'],
                'timestamp': datetime.now(timezone.utc).isoformat()
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
