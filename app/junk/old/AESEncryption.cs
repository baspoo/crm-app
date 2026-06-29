using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SimpleHashEncrypt
{
	public static string Encrypt(string plainText, string key)
	{
		byte[] data = Encoding.UTF8.GetBytes(plainText);
		byte[] keyBytes = Encoding.UTF8.GetBytes(key);

		for (int i = 0; i < data.Length; i++)
		{
			data[i] ^= keyBytes[i % keyBytes.Length]; // XOR
		}

		return Convert.ToBase64String(data);
	}

	public static string Decrypt(string cipherText, string key)
	{
		byte[] data = Convert.FromBase64String(cipherText);
		byte[] keyBytes = Encoding.UTF8.GetBytes(key);

		for (int i = 0; i < data.Length; i++)
		{
			data[i] ^= keyBytes[i % keyBytes.Length]; // XOR
		}

		return Encoding.UTF8.GetString(data);
	}
}

public class HASHEncrypt
{

	public static string Encrypt(string plainText, string seedString)
	{
		string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));

		// แยก padding
		string padding = "";
		if (base64.EndsWith("==")) { padding = "=="; base64 = base64[..^2]; }
		else if (base64.EndsWith("=")) { padding = "="; base64 = base64[..^1]; }

		char[] arr = base64.ToCharArray();
		Shuffle(arr, GetSeedFromString(seedString));

		return new string(arr) + padding;
	}

	public static string Decrypt(string shuffledBase64, string seedString)
	{
		string padding = "";
		if (shuffledBase64.EndsWith("==")) { padding = "=="; shuffledBase64 = shuffledBase64[..^2]; }
		else if (shuffledBase64.EndsWith("=")) { padding = "="; shuffledBase64 = shuffledBase64[..^1]; }

		char[] arr = shuffledBase64.ToCharArray();
		Unshuffle(arr, GetSeedFromString(seedString));

		string base64 = new string(arr) + padding;
		return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
	}

	// สร้าง seed (int) จาก string
	private static int GetSeedFromString(string seedString)
	{
		using (MD5 md5 = MD5.Create())
		{
			byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(seedString));
			return BitConverter.ToInt32(hash, 0); // ใช้ 4 byte แรกเป็น int
		}
	}

	private static void Shuffle(char[] array, int seed)
	{
		Random rand = new Random(seed);
		for (int i = array.Length - 1; i > 0; i--)
		{
			int j = rand.Next(i + 1);
			(array[i], array[j]) = (array[j], array[i]);
		}
	}

	private static void Unshuffle(char[] array, int seed)
	{
		Random rand = new Random(seed);
		int[] indices = new int[array.Length];
		for (int i = 0; i < array.Length; i++) indices[i] = i;

		// จำลำดับการสลับ
		for (int i = indices.Length - 1; i > 0; i--)
		{
			int j = rand.Next(i + 1);
			(indices[i], indices[j]) = (indices[j], indices[i]);
		}

		// คืนค่า
		char[] copy = (char[])array.Clone();
		for (int i = 0; i < array.Length; i++)
		{
			array[indices[i]] = copy[i];
		}
	}
}





public static class AESEncryption
{
	/// <summary>
	/// A class containing AES-encrypted text, plus the IV value required to decrypt it (with the correct password)
	/// </summary>
	public struct AESEncryptedText
	{
		public string IV;
		public string EncryptedText;
	}

	/// <summary>
	/// Encrypts a given text string with a password
	/// </summary>
	/// <param name="plainText">The text to encrypt</param>
	/// <param name="password">The password which will be required to decrypt it</param>
	/// <returns>An AESEncryptedText object containing the encrypted string and the IV value required to decrypt it.</returns>
	public static AESEncryptedText Encrypt(string plainText, string password)
	{
		using (var aes = Aes.Create())
		{
			//password = "l4Bo2Bzrien9EekF1tF2FttvbppUzu1g";
			var hashPassword = OnHash(password);
			aes.GenerateIV();  
			aes.BlockSize = 128;
			aes.KeySize = 256;
			aes.Key = Encoding.UTF8.GetBytes(hashPassword); //ConvertToKeyBytes(password);
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;

			var textBytes = Encoding.UTF8.GetBytes(plainText);

			var aesEncryptor = aes.CreateEncryptor();
			var encryptedBytes = aesEncryptor.TransformFinalBlock(textBytes, 0, textBytes.Length);

			return new AESEncryptedText
			{
				IV = Convert.ToBase64String(aes.IV),
				EncryptedText = Convert.ToBase64String(encryptedBytes)
			};
		}
	}

	/// <summary>
	/// Decrypts an AESEncryptedText with a password
	/// </summary>
	/// <param name="encryptedText">The AESEncryptedText object to decrypt</param>
	/// <param name="password">The password to use when decrypting</param>
	/// <returns>The original plainText string.</returns>
	public static string Decrypt(AESEncryptedText encryptedText, string password)
	{
		return Decrypt(encryptedText.EncryptedText, encryptedText.IV, password);
	}

	/// <summary>
	/// Decrypts an encrypted string with an IV value password
	/// </summary>
	/// <param name="encryptedText">The encrypted string to be decrypted</param>
	/// <param name="iv">The IV value which was generated when the text was encrypted</param>
	/// <param name="password">The password to use when decrypting</param>
	/// <returns>The original plainText string.</returns>
	public static string Decrypt(string encryptedText, string iv, string password)
	{
		using (Aes aes = Aes.Create())
		{
			var ivBytes = Convert.FromBase64String(iv);
			var encryptedTextBytes = Convert.FromBase64String(encryptedText);

			var decryptor = aes.CreateDecryptor(ConvertToKeyBytes(password), ivBytes);
			var decryptedBytes = decryptor.TransformFinalBlock(encryptedTextBytes, 0, encryptedTextBytes.Length);

			return Encoding.UTF8.GetString(decryptedBytes);
		}
	}


	public static byte[] StringToByteArray(string hex)
	{
		return Enumerable.Range(0, hex.Length)
						 .Where(x => x % 2 == 0)
						 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
						 .ToArray();
	}

	// Ensure the AES key byte-array is the right size - AES will reject it otherwise
	public static byte[] ConvertToKeyBytes(string password)
	{
		var hexBytes = StringToByteArray(password);
		return hexBytes;
		//var bytes = System.Convert.FromBase64String(password);
		//return System.Convert.FromBase64String(password);
	}

	#region ConvertAgentKey
	public static string OnHash(string key)
	{
		// Create a SHA256   
		using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
		{
			// ComputeHash - returns byte array  
			byte[] bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));

			// Convert byte array to a string   
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			for (int i = 0; i < bytes.Length; i++)
			{
				builder.Append(bytes[i].ToString("x2"));
			}

			//Debug.Log(builder.ToString());
			//Debug.Log("c239015f1fa9276ab0237226c97975a5");
			return builder.ToString();
		}
	}
	#endregion

}












#if UNITY_EDITOR
public class AESEncryptionEditor : EditorWindow {

	[MenuItem("REFLEX/Editor/AESEncryptionEditor")]
	public static void ShowWindow()
	{
		var window = EditorWindow.GetWindow(typeof(AESEncryptionEditor));
		window.title = "AES-Encryption Editor";
	}



	void Update()
	{
		Repaint();
	}

 
	string textInputt = "";
	string textOut = "";
	string PIN;
	static EditorGUIService.GUIData gui = new EditorGUIService.GUIData("", EditorGUIService.GUIData.castTime.realTime);
	void OnGUI()
    {

		PIN = gui.TextArea("PIN", true);
		gui.Space(10);
		textInputt = gui.TextArea("Input", textInputt , 80);
		gui.Space(10);
		textOut = gui.TextArea("Output", textOut,80);
		gui.Space(10);
		gui.Button("Encrypt", ()=> {
			textOut = HASHEncrypt.Encrypt(textInputt, PIN);
		});
		gui.Button("Decrypt", () => {
			textOut = HASHEncrypt.Decrypt(textInputt, PIN);
		});


	}

}
#endif
