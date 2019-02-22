import json


def handler(event, context):
    response = {
        "statusCode": 200,
        "body": json.loads(event['body'])['username']
    }

    return response
