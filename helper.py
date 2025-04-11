import argparse
import pandas as pd
import openpyxl
import csv
from dotenv import load_dotenv
import os
import boto3

load_dotenv()

input_file = "C:\\Users\\aqudei\\Downloads\\SmarkPhotShop\\LOCK_DECORATIVE_MAGNET_KEYCHAIN_BOTTLE_OPENER_SHIRT - Excel.xlsx"
skipped = ["SKU"]

def delete_all_s3_objects(bucket_name, prefix=''):
    s3 = boto3.client('s3')
    paginator = s3.get_paginator('list_objects_v2')

    for page in paginator.paginate(Bucket=bucket_name, Prefix=prefix):
        objects = [{'Key': obj['Key']} for obj in page.get('Contents', [])]
        if objects:
            print(f"Deleting {len(objects)} objects...")
            s3.delete_objects(
                Bucket=bucket_name,
                Delete={'Objects': objects}
            )
            
def list_s3_files_recursive(bucket_name, prefix=""):
    s3 = boto3.client("s3")
    paginator = s3.get_paginator("list_objects_v2")

    file_list = []

    for page in paginator.paginate(Bucket=bucket_name, Prefix=prefix):
        for obj in page.get("Contents", []):
            file_list.append(obj["Key"])

    return file_list


if __name__ == "__main__":
    bucket_name = os.getenv("BUCKET")
    prefix = ""

    # delete_all_s3_objects(bucket_name,prefix)
    files = list_s3_files_recursive(bucket_name,prefix)
    for f in files:
        print(f)