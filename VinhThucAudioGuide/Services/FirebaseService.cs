using Firebase.Database;
using Firebase.Database.Query;
using System.Collections.Generic;
using System.Threading.Tasks;
using VinhThucAudioGuide.Models;

namespace VinhThucAudioGuide.Services;

public class FirebaseService
{
    private readonly FirebaseClient firebase = new FirebaseClient("LINK_FIREBASE_CUA_SEP_VAO_DAY");

    public async Task<List<DiemThamQuan>> LayDuLieuAsync()
    {
        var data = await firebase.Child("DanhSachDiemThamQuan").OnceAsync<DiemThamQuan>();
        var danhSach = new List<DiemThamQuan>();
        foreach (var item in data)
        {
            var diem = item.Object;
            diem.Id = item.Key;
            danhSach.Add(diem);
        }
        return danhSach;
    }
}