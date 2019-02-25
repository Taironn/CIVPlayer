import os
import boto3
import json
from datetime import datetime


def avarage_steps(event, context):
    client = boto3.client('dynamodb')
    steps = _list_steps(client)

    statistics = _calculate_statistic(steps)

    response = {
        'statusCode': 200,
        'body': json.dumps(statistics)
    }

    return response


def _calculate_statistic(steps):
    last_step = None
    user_statistics = {}
    sorted_steps = sorted(steps, key=lambda k: _get_timestamp(k))
    for step in sorted_steps:
        username = _get_username(step)
        timestamp = _get_timestamp(step)
        if username not in user_statistics.keys():
            user_statistics[username] = {
                'count': 0,
                'avarage': 0
            }
        if last_step is not None:
            N = user_statistics[username]['count']
            avg = user_statistics[username]['avarage']
            new_avg = (N * avg + (timestamp -
                                  _get_timestamp(last_step)).total_seconds()) / (N + 1)
            user_statistics[username]['count'] = N + 1
            user_statistics[username]['avarage'] = new_avg

        last_step = step

    return {
        'last_step': last_step,
        'user_statistics': user_statistics
    }


def _list_steps(client):
    return client.scan(
        TableName=os.environ['TableName'],
        Select='ALL_ATTRIBUTES'
    )['Items']


def _get_username(step):
    return step['username']['S']


def _get_timestamp(step):
    datestring = step['timestamp']['S']
    return datetime.fromisoformat(datestring)
