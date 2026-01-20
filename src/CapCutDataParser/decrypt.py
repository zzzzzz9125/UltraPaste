
 
import argparse
import sys
import base64
from Crypto.Cipher import AES
import random

def decrypt_aes_gcm(encoded_data: str, key: bytes, iv: bytes) -> bytes | None:
    try:
        decoded_data = base64.b64decode(encoded_data)

        tag_size = 16
        if len(decoded_data) < tag_size:
            print("Error: Decoded data is shorter than the GCM tag size.")
            return None

        ciphertext = decoded_data[:-tag_size]
        tag = decoded_data[-tag_size:]

        cipher = AES.new(key, AES.MODE_GCM, iv)
        plaintext = cipher.decrypt_and_verify(ciphertext, tag)
        return plaintext
    except Exception as e:
        return None

def extract_key_iv(input: str) -> tuple[str, str]:
    if len(input) < 131:
        raise ValueError("Input string is too short to extract key and IV.")
    
    offsets = [0, 7, 20, 33, 40, 47, 59, 66, 76, 89, 99, 127]
    extracted_chars = []
    input_list = list(input)

    for offset in reversed(offsets):
        if offset + 3 < len(input_list):
            chars = input_list[offset:offset + 4]
            extracted_chars[:0] = chars 
            del input_list[offset:offset + 4]
        else:
            pass

    key_iv = ''.join(extracted_chars)
    new_input = ''.join(input_list)
    return key_iv, new_input


def decrypt_string(input: str) -> str | None:
    key_iv, input = extract_key_iv(input)
    key = key_iv[:32]
    iv = key_iv[32:]

    decrypted_text = decrypt_aes_gcm(input, key.encode(), iv.encode())
    if decrypted_text is None:
        print("Decryption failed or data is tampered.")
        return None
    
    return decrypted_text.decode()

def encrypt_aes_gcm(plaintext: bytes, key: bytes, iv: bytes) -> str | None:
    try:
        cipher = AES.new(key, AES.MODE_GCM, iv)
        ciphertext, tag = cipher.encrypt_and_digest(plaintext)
        encoded_data = base64.b64encode(ciphertext + tag).decode()
        return encoded_data
    except Exception as e:
        return None

def insert_key_iv(new_input: str, key_iv: str) -> str:
    if len(new_input) < 90:
        raise ValueError("Input string is too short to insert key and IV.")
    
    offsets = [0, 7, 20, 33, 40, 47, 59, 66, 76, 89, 99, 127]
    input_list = list(new_input)
    idx = 0
    for offset in offsets:
        if idx + 4 <= len(key_iv):
            chars = key_iv[idx:idx+4]
            input_list[offset:offset] = chars
            idx += 4
        else:
            pass

    return ''.join(input_list)

def encrypt_string(input: str) -> str | None:
    # 使用随机数生成key和iv，查表 "0123456789abcdefghijABCDEFGHIJ"
    table = "0123456789abcdefghijABCDEFGHIJ"
    key = ''.join(random.choice(table) for _ in range(32))
    iv = ''.join(random.choice(table) for _ in range(16))
    key_iv = key + iv

    encrypted_str = encrypt_aes_gcm(input.encode(), key.encode(), iv.encode())
    if encrypted_str is None:
        return None
    
    # 将key_iv嵌入到加密字符串中
    result = insert_key_iv(encrypted_str, key_iv)
    return result

def test_decrypt():
    input = ""
    
    # 原版同样问题，太短无法解密
    #input = "F32JygrD98GJXbYgEiZ4DFHEQV9X+LMOJ5iC8chwHGi9Gmpj991i"
    result = decrypt_string(input)
    if result:
        print("Decryption successful.")
    else:
        print("Decryption failed.")

def test():
    text = 'Hello, World!Hello, World!Hello, World!Hello, World!'
    encrypted = encrypt_string(text)
    if encrypted is None:
        print("Encryption failed.")
        return
    print("Encrypted:", encrypted)
    decrypted = decrypt_string(encrypted)
    if decrypted is None:
        print("Decryption failed.")
        return
    print("Decrypted:", decrypted)
    assert decrypted == text, "Decrypted text does not match original!"

def process_file(input_file: str, output_file: str | None, decrypt: bool = True):
    try:
        with open(input_file, 'r', encoding='utf-8') as f:
            content = f.read().strip()
    except FileNotFoundError:
        print(f"Error: File '{input_file}' not found.")
        sys.exit(1)
    except Exception as e:
        print(f"Error reading file '{input_file}': {e}")
        sys.exit(1)

    if decrypt:
        result = decrypt_string(content)
        action = "Decrypted"
    else:
        result = encrypt_string(content)
        action = "Encrypted"

    if result is None:
        print(f"{action} failed.")
        sys.exit(1)

    if output_file:
        try:
            with open(output_file, 'w', encoding='utf-8') as f:
                f.write(result)
            print(f"{action} successfully. Output written to '{output_file}'")
        except Exception as e:
            print(f"Error writing to file '{output_file}': {e}")
            sys.exit(1)
    else:
        print(f"{action} successfully.")
        print("Result:")
        print(result)

def main():
    parser = argparse.ArgumentParser(description='Encrypt or decrypt strings using AES-GCM')
    parser.add_argument('-f', '--file', required=True, help='Input file to process')
    parser.add_argument('-o', '--output', help='Output file (optional, defaults to stdout)')
    parser.add_argument('-e', '--encrypt', action='store_true', help='Encrypt the input')
    parser.add_argument('-d', '--decrypt', action='store_true', help='Decrypt the input (default)')
    
    args = parser.parse_args()

    if not args.encrypt and not args.decrypt:
        args.decrypt = True

    if args.encrypt and args.decrypt:
        print("Error: Cannot specify both --encrypt and --decrypt")
        sys.exit(1)

    process_file(args.file, args.output, decrypt=args.decrypt)

if __name__ == '__main__':
    main()