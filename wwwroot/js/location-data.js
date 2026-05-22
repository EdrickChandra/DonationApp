var kotaByProvinsi = {
    "Aceh": ["Banda Aceh", "Sabang", "Langsa", "Lhokseumawe", "Subulussalam", "Aceh Besar", "Aceh Utara", "Aceh Timur", "Bireuen", "Pidie"],
    "Bali": ["Denpasar", "Badung", "Gianyar", "Tabanan", "Singaraja", "Klungkung", "Bangli", "Karangasem", "Jembrana"],
    "Banten": ["Serang", "Cilegon", "Tangerang", "Tangerang Selatan", "Lebak", "Pandeglang"],
    "Bengkulu": ["Bengkulu", "Rejang Lebong", "Seluma", "Kaur", "Mukomuko"],
    "DI Yogyakarta": ["Yogyakarta", "Sleman", "Bantul", "Gunung Kidul", "Kulon Progo"],
    "DKI Jakarta": ["Jakarta Pusat", "Jakarta Utara", "Jakarta Barat", "Jakarta Selatan", "Jakarta Timur"],
    "Gorontalo": ["Gorontalo", "Bone Bolango", "Pohuwato", "Boalemo", "Gorontalo Utara"],
    "Jambi": ["Jambi", "Batanghari", "Bungo", "Kerinci", "Merangin", "Muaro Jambi", "Sarolangun", "Tanjung Jabung Barat", "Tanjung Jabung Timur"],
    "Jawa Barat": ["Bandung", "Bekasi", "Bogor", "Cimahi", "Cirebon", "Depok", "Sukabumi", "Tasikmalaya", "Banjar", "Karawang", "Subang", "Indramayu", "Majalengka", "Kuningan", "Garut", "Ciamis", "Sumedang"],
    "Jawa Tengah": ["Semarang", "Solo", "Magelang", "Pekalongan", "Salatiga", "Tegal", "Klaten", "Kudus", "Boyolali", "Purwokerto", "Cilacap", "Kebumen", "Wonosobo", "Temanggung", "Demak", "Jepara", "Blora", "Rembang", "Pati", "Grobogan", "Purworejo", "Banjarnegara", "Batang", "Pemalang", "Brebes"],
    "Jawa Timur": ["Surabaya", "Malang", "Kediri", "Blitar", "Madiun", "Mojokerto", "Pasuruan", "Probolinggo", "Batu", "Jember", "Banyuwangi", "Situbondo", "Bondowoso", "Lumajang", "Jombang", "Nganjuk", "Bojonegoro", "Tuban", "Lamongan", "Gresik", "Sidoarjo", "Bangkalan", "Sampang", "Pamekasan", "Sumenep", "Tulungagung", "Trenggalek", "Ponorogo", "Magetan", "Ngawi", "Pacitan"],
    "Kalimantan Barat": ["Pontianak", "Singkawang", "Sambas", "Ketapang", "Sanggau", "Sintang", "Kapuas Hulu", "Melawi", "Bengkayang", "Landak", "Sekadau", "Mempawah", "Kubu Raya", "Kayong Utara"],
    "Kalimantan Selatan": ["Banjarmasin", "Banjarbaru", "Kotabaru", "Tabalong", "Tapin", "Hulu Sungai Selatan", "Hulu Sungai Tengah", "Hulu Sungai Utara", "Balangan", "Tanah Bumbu", "Tanah Laut", "Barito Kuala", "Banjar"],
    "Kalimantan Tengah": ["Palangka Raya", "Kotawaringin Barat", "Kotawaringin Timur", "Kapuas", "Barito Selatan", "Barito Utara", "Barito Timur", "Murung Raya", "Gunung Mas", "Katingan", "Seruyan", "Lamandau", "Sukamara", "Pulang Pisau"],
    "Kalimantan Timur": ["Samarinda", "Balikpapan", "Bontang", "Kutai Kartanegara", "Berau", "Kutai Barat", "Kutai Timur", "Mahakam Ulu", "Penajam Paser Utara", "Paser"],
    "Kalimantan Utara": ["Tarakan", "Nunukan", "Bulungan", "Malinau", "Tana Tidung"],
    "Kepulauan Bangka Belitung": ["Pangkal Pinang", "Bangka", "Bangka Barat", "Bangka Tengah", "Bangka Selatan", "Belitung", "Belitung Timur"],
    "Kepulauan Riau": ["Batam", "Tanjung Pinang", "Bintan", "Karimun", "Natuna", "Lingga", "Kepulauan Anambas"],
    "Lampung": ["Bandar Lampung", "Metro", "Lampung Selatan", "Lampung Tengah", "Lampung Utara", "Lampung Barat", "Lampung Timur", "Tanggamus", "Tulang Bawang", "Pesawaran", "Pringsewu", "Mesuji", "Tulang Bawang Barat", "Pesisir Barat", "Way Kanan"],
    "Maluku": ["Ambon", "Tual", "Maluku Tengah", "Maluku Tenggara", "Maluku Barat Daya", "Seram Bagian Barat", "Seram Bagian Timur", "Buru", "Buru Selatan", "Kepulauan Aru"],
    "Maluku Utara": ["Ternate", "Tidore Kepulauan", "Halmahera Barat", "Halmahera Tengah", "Halmahera Utara", "Halmahera Selatan", "Kepulauan Sula", "Halmahera Timur", "Pulau Morotai", "Pulau Taliabu"],
    "Nusa Tenggara Barat": ["Mataram", "Bima", "Lombok Barat", "Lombok Tengah", "Lombok Timur", "Lombok Utara", "Sumbawa", "Sumbawa Barat", "Dompu"],
    "Nusa Tenggara Timur": ["Kupang", "Ende", "Sikka", "Flores Timur", "Manggarai", "Manggarai Barat", "Manggarai Timur", "Ngada", "Nagekeo", "Timor Tengah Selatan", "Timor Tengah Utara", "Belu", "Alor", "Lembata", "Rote Ndao", "Sabu Raijua", "Malaka", "Sumba Barat", "Sumba Timur", "Sumba Barat Daya", "Sumba Tengah"],
    "Papua": ["Jayapura", "Merauke", "Biak", "Mimika", "Nabire", "Sarmi", "Keerom", "Waropen", "Supiori", "Puncak Jaya"],
    "Papua Barat": ["Manokwari", "Sorong", "Raja Ampat", "Fakfak", "Kaimana", "Teluk Bintuni", "Teluk Wondama", "Maybrat", "Tambrauw"],
    "Riau": ["Pekanbaru", "Dumai", "Bengkalis", "Indragiri Hilir", "Indragiri Hulu", "Kampar", "Kuantan Singingi", "Pelalawan", "Rokan Hilir", "Rokan Hulu", "Siak", "Kepulauan Meranti"],
    "Sulawesi Barat": ["Mamuju", "Polewali Mandar", "Majene", "Mamasa", "Pasangkayu"],
    "Sulawesi Selatan": ["Makassar", "Parepare", "Palopo", "Bone", "Gowa", "Maros", "Pangkajene", "Barru", "Soppeng", "Wajo", "Sidrap", "Pinrang", "Enrekang", "Luwu", "Luwu Utara", "Luwu Timur", "Tana Toraja", "Toraja Utara", "Bulukumba", "Bantaeng", "Jeneponto", "Takalar", "Selayar", "Sinjai"],
    "Sulawesi Tengah": ["Palu", "Tolitoli", "Poso", "Morowali", "Banggai", "Donggala", "Buol", "Parigi Moutong", "Tojo Una-Una", "Sigi", "Morowali Utara"],
    "Sulawesi Tenggara": ["Kendari", "Bau-Bau", "Kolaka", "Konawe", "Konawe Selatan", "Konawe Utara", "Kolaka Utara", "Kolaka Timur", "Bombana", "Buton", "Muna", "Wakatobi"],
    "Sulawesi Utara": ["Manado", "Bitung", "Tomohon", "Kotamobagu", "Minahasa", "Minahasa Utara", "Minahasa Selatan", "Minahasa Tenggara", "Bolaang Mongondow", "Kepulauan Sangihe", "Kepulauan Talaud"],
    "Sumatera Barat": ["Padang", "Bukittinggi", "Payakumbuh", "Solok", "Padang Panjang", "Sawahlunto", "Pariaman", "Agam", "Tanah Datar", "Padang Pariaman", "Lima Puluh Kota", "Sijunjung", "Dharmasraya", "Solok Selatan", "Pasaman", "Pasaman Barat", "Pesisir Selatan", "Kepulauan Mentawai"],
    "Sumatera Selatan": ["Palembang", "Lubuk Linggau", "Prabumulih", "Pagaralam", "Banyuasin", "Lahat", "Muara Enim", "Musi Banyuasin", "Musi Rawas", "Ogan Ilir", "Ogan Komering Ilir", "Ogan Komering Ulu", "Ogan Komering Ulu Selatan", "Ogan Komering Ulu Timur", "Empat Lawang"],
    "Sumatera Utara": ["Medan", "Binjai", "Tebing Tinggi", "Pematang Siantar", "Sibolga", "Padang Sidempuan", "Gunungsitoli", "Deli Serdang", "Langkat", "Karo", "Simalungun", "Asahan", "Labuhanbatu", "Tapanuli Utara", "Tapanuli Tengah", "Tapanuli Selatan", "Mandailing Natal", "Nias", "Nias Selatan", "Dairi", "Batu Bara", "Padang Lawas", "Padang Lawas Utara"]
};

function populateKota(provinsi, kotaSelectEl, selectedKota) {
    kotaSelectEl.innerHTML = '<option value="">Pilih Kota</option>';
    var cities = kotaByProvinsi[provinsi];
    if (!cities) return;
    cities.forEach(function (kota) {
        var opt = document.createElement('option');
        opt.value = kota;
        opt.textContent = kota;
        if (selectedKota && kota === selectedKota) opt.selected = true;
        kotaSelectEl.appendChild(opt);
    });
}